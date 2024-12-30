using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Models;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ARInvoiceController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ARInvoiceController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/arinvoice")]
        [Authorize]
        public IActionResult GetARInvoice([FromBody] ARInvoiceParameter parameter)
        {
            Data data = new Data();
            ARInvoice arInvoice = new ARInvoice();
            ARInvoiceDetail arInvoiceDetail = new ARInvoiceDetail();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_ARINV_HEADER('" + parameter.sfaRefrenceNum + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "AR Invoice not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    List<ARInvoiceDetail> listARInvoiceDetail = new List<ARInvoiceDetail>();

                                    int docEntry;
                                    docEntry = Convert.ToInt32(reader["docEntry"]);

                                    string queryStringDetail = "CALL SOL_SP_ADDON_SFA_INT_GET_ARINV_DETAIL(" + docEntry + ")";

                                    using (var commandDetail = new HanaCommand(queryStringDetail, connection))
                                    {
                                        using (var readerDetail = commandDetail.ExecuteReader())
                                        {
                                            if (!readerDetail.HasRows)
                                            {
                                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                                {
                                                    responseCode = "404",
                                                    responseMessage = "AR Invoice Detail not found.",

                                                });
                                            }
                                            else
                                            {
                                                while (readerDetail.Read())
                                                {
                                                    arInvoiceDetail = new ARInvoiceDetail
                                                    {
                                                        kodeProduk = readerDetail["kodeProduk"].ToString(),
                                                        qtyInPcs = Convert.ToDecimal(readerDetail["qty"]),
                                                        priceValue = Convert.ToDecimal(readerDetail["priceValue"]),
                                                        discountValue = Convert.ToDecimal(readerDetail["discountValue"])
                                                    };

                                                    listARInvoiceDetail.Add(arInvoiceDetail);
                                                }
                                            }
                                        }
                                    }

                                    arInvoice = new ARInvoice
                                    {
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        noInvoiceERP = reader["noInvoiceERP"].ToString(),
                                        tanggalInvoice = reader["tanggalInvoice"].ToString(),
                                        detail = listARInvoiceDetail,
                                        kodeCabang = reader["kodeCabang"].ToString(),
                                        invoiceType = reader["invoiceType"].ToString(),
                                        invoiceAmount = Convert.ToDecimal(reader["invoiceAmount"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNumber"].ToString(),
                                    };
                                }

                                data = new Data
                                {
                                    data = arInvoice
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
