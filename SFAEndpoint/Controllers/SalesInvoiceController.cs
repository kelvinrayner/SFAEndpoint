using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesInvoiceController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public SalesInvoiceController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/salesinvoice/master")]
        [Authorize]
        public IActionResult GetSalesInvoice()
        {
            Data data = new Data();
            SalesInvoice salesInvoice = new SalesInvoice();
            SalesInvoiceDetail salesInvoiceDetail = new SalesInvoiceDetail();

            List<SalesInvoice> listSalesInvoice = new List<SalesInvoice>();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_SALES_INVOICE_HEADER()";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Sales Invoice not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    List<SalesInvoiceDetail> listSalesInvoiceDetail = new List<SalesInvoiceDetail>();

                                    int docEntry;
                                    docEntry = Convert.ToInt32(reader["docEntry"]);

                                    string invoiceType = reader["invoiceType"].ToString();

                                    if (invoiceType == "INV")
                                    {
                                        string queryStringDetail = "CALL SOL_SP_ADDON_SFA_INT_GET_INVOICE_DETAIL(" + docEntry + ")";

                                        using (var commandDetail = new HanaCommand(queryStringDetail, connection))
                                        {
                                            using (var readerDetail = commandDetail.ExecuteReader())
                                            {
                                                if (readerDetail.HasRows)
                                                {
                                                    while (readerDetail.Read())
                                                    {
                                                        salesInvoiceDetail = new SalesInvoiceDetail
                                                        {
                                                            lineNumSAP = Convert.ToInt32(readerDetail["lineNumSAP"]),
                                                            kodeProduk = readerDetail["kodeProduk"].ToString(),
                                                            kodeProdukPrincipal = readerDetail["kodeProdukPrincipal"].ToString(),
                                                            qtyInPcs = Convert.ToDecimal(readerDetail["qty"]),
                                                            priceValue = Convert.ToDecimal(readerDetail["priceValue"]),
                                                            discountValue = Convert.ToDecimal(readerDetail["discountValue"])
                                                        };

                                                        listSalesInvoiceDetail.Add(salesInvoiceDetail);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string queryStringDetail = "CALL SOL_SP_ADDON_SFA_INT_GET_RETUR_DETAIL(" + docEntry + ")";

                                        using (var commandDetail = new HanaCommand(queryStringDetail, connection))
                                        {
                                            using (var readerDetail = commandDetail.ExecuteReader())
                                            {
                                                if (readerDetail.HasRows)
                                                {
                                                    while (readerDetail.Read())
                                                    {
                                                        salesInvoiceDetail = new SalesInvoiceDetail
                                                        {
                                                            lineNumSAP = Convert.ToInt32(readerDetail["lineNumSAP"]),
                                                            kodeProduk = readerDetail["kodeProduk"].ToString(),
                                                            kodeProdukPrincipal = readerDetail["kodeProdukPrincipal"].ToString(),
                                                            qtyInPcs = Convert.ToDecimal(readerDetail["qty"]),
                                                            priceValue = Convert.ToDecimal(readerDetail["priceValue"]),
                                                            discountValue = Convert.ToDecimal(readerDetail["discountValue"])
                                                        };

                                                        listSalesInvoiceDetail.Add(salesInvoiceDetail);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    salesInvoice = new SalesInvoice
                                    {
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        orderNoERP = reader["orderNoERP"].ToString(),
                                        orderDateERP = Convert.ToDateTime(reader["orderDateERP"]),
                                        noInvoiceERP = reader["noInvoiceERP"].ToString(),
                                        tanggalInvoice = reader["tanggalInvoice"].ToString(),
                                        detail = listSalesInvoiceDetail,
                                        kodeCabang = reader["kodeCabang"].ToString(),
                                        invoiceType = reader["invoiceType"].ToString(),
                                        invoiceAmount = Convert.ToDecimal(reader["invoiceAmount"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNum"].ToString(),
                                    };

                                    listSalesInvoice.Add(salesInvoice);
                                }

                                data = new Data
                                {
                                    data = listSalesInvoice
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
