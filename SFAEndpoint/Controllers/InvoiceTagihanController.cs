using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceTagihanController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public InvoiceTagihanController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/invoicetagihan/master")]
        [Authorize]
        public IActionResult Index()
        {
            Data data = new Data();
            InvoiceTagihan invoiceTagihan = new InvoiceTagihan();

            List<InvoiceTagihan> listInvoiceTagihan = new List<InvoiceTagihan>();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_INVOICE()";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Invoice Tagihan not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    invoiceTagihan = new InvoiceTagihan
                                    {
                                        docEntrySAP = Convert.ToInt32(reader["docEntry"]),
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        noInvoice = reader["noInvoice"].ToString(),
                                        tanggalInvoice = Convert.ToDateTime(reader["tanggalInvoice"]),
                                        tanggalInvoiceJatuhTempo = Convert.ToDateTime(reader["tanggalInvoiceJatuhTempo"]),
                                        nilaiInvoice = Convert.ToDecimal(reader["nilaiInvoice"]),
                                        nilaiInvoiceTerbayar = Convert.ToDecimal(reader["nilaiInvoiceTerbayar"]),
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        kodeCabang = reader["kodeCabang"].ToString(),
                                        invoiceType = reader["invoiceType"].ToString(),
                                        tanggalTagih = reader["tanggalTagih"].ToString(),
                                    };

                                    listInvoiceTagihan.Add(invoiceTagihan);
                                }

                                data = new Data
                                {
                                    data = listInvoiceTagihan
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
