using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingPaymentController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public IncomingPaymentController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/incomingpayment")]
        [Authorize]
        public IActionResult GetIncoming([FromBody] IncomingPaymentParameter parameter)
        {
            Data data = new Data();
            IncomingPayment incomingPayment = new IncomingPayment();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_INCOMING_PAY('" + parameter.sfaRefrenceNum + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Incoming Payment not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    incomingPayment = new IncomingPayment
                                    {
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        noInvoiceERP = reader["noInvoiceERP"].ToString(),
                                        tanggalInvoice = Convert.ToDateTime(reader["tanggalInvoice"]),
                                        invoiceAmount = Convert.ToDecimal(reader["invoiceAmount"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNumber"].ToString()
                                    };
                                }

                                data = new Data
                                {
                                    data = incomingPayment
                                };
                            }
                        }
                    }
                    connection.Close();
                }
                return Ok(data);
            }
            catch (HanaException hx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = "HANA Error: " + hx.Message,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message,

                });
            }
        }
    }
}
