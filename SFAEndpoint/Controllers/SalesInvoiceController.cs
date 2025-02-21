using System;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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
                                    salesInvoice = new SalesInvoice
                                    {
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        orderNoERP = reader["orderNoERP"].ToString(),
                                        orderDateERP = reader["orderDateERP"].ToString(),
                                        noInvoiceERP = reader["noInvoiceERP"].ToString(),
                                        tanggalInvoice = reader["tanggalInvoice"].ToString(),
                                        lineNumSAP = Convert.ToInt32(reader["lineNumSAP"]),
                                        kodeProduk = reader["kodeProduk"].ToString(),
                                        kodeProdukPrincipal = reader["kodeProdukPrincipal"].ToString(),
                                        qtyInPcs = Convert.ToDecimal(reader["qty"]),
                                        priceValue = Convert.ToDecimal(reader["priceValue"]),
                                        discountValue = Convert.ToDecimal(reader["discountValue"]),
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
