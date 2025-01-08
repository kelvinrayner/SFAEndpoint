using System;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Sap.Data.Hana;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrderController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public SalesOrderController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/salesorder")]
        [Authorize]
        public IActionResult GetARInvoice([FromBody] GetSalesOrderParameter parameter)
        {
            Data data = new Data();
            SalesOrder salesOrder = new SalesOrder();
            SalesOrderDetail salesOrderDetail = new SalesOrderDetail();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_SO_HEADER('" + parameter.sfaRefrenceNumber + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Sales Order not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    List<SalesOrderDetail> listSalesOrderDetail = new List<SalesOrderDetail>();

                                    int docEntry;
                                    docEntry = Convert.ToInt32(reader["docEntry"]);

                                    string queryStringDetail = "CALL SOL_SP_ADDON_SFA_INT_GET_SO_DETAIL(" + docEntry + ")";

                                    using (var commandDetail = new HanaCommand(queryStringDetail, connection))
                                    {
                                        using (var readerDetail = commandDetail.ExecuteReader())
                                        {
                                            if (!readerDetail.HasRows)
                                            {
                                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                                {
                                                    responseCode = "404",
                                                    responseMessage = "Sales Order Detail not found.",

                                                });
                                            }
                                            else
                                            {
                                                while (readerDetail.Read())
                                                {
                                                    salesOrderDetail = new SalesOrderDetail
                                                    {
                                                        kodeProduk = readerDetail["kodeProduk"].ToString(),
                                                        qtyInPcs = Convert.ToDouble(readerDetail["qty"]),
                                                        priceValue = Convert.ToDouble(readerDetail["priceValue"]),
                                                        discountValue = Convert.ToDouble(readerDetail["discountValue"])
                                                    };

                                                    listSalesOrderDetail.Add(salesOrderDetail);
                                                }
                                            }
                                        }
                                    }

                                    salesOrder = new SalesOrder
                                    {
                                        docEntrySAP = docEntry,
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        noSalesOrderERP = reader["noSalesOrderERP"].ToString(),
                                        tanggalSalesOrder = reader["tanggalSalesOrder"].ToString(),
                                        detail = listSalesOrderDetail,
                                        kodeCabang = reader["kodeCabang"].ToString(),
                                        salesOrderAmount = Convert.ToDouble(reader["salesOrderAmount"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNumber"].ToString(),
                                    };
                                }

                                data = new Data
                                {
                                    data = salesOrder
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

        [HttpPost("/sapapi/sfaintegration/salesorder/new")]
        [Authorize]
        public IActionResult PostSalesOrder([FromBody] SalesOrderParameter parameter)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            string wilayah = "";
            string productGroup = "";
            string brand = "";
            string salesPerson = "";
            string customerGroup = "";

            DateTime tanggal = parameter.tanggal.ToDateTime(TimeOnly.MinValue);

            try
            {
                var connection = new HanaConnection(_connectionStringHana);

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_FMS_WILAYAH_SO(" + parameter.salesCode + ")";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    wilayah = reader["U_SOL_WILAYAH"].ToString();
                                }
                            }
                        }
                    }

                    string queryStringSalesPerson = "CALL SOL_SP_ADDON_SFA_INT_FMS_SALES_PERSON_SO('" + parameter.salesCode + "')";

                    using (var commandSalesPerson = new HanaCommand(queryStringSalesPerson, connection))
                    {
                        using (var readerSalesPerson = commandSalesPerson.ExecuteReader())
                        {
                            if (readerSalesPerson.HasRows)
                            {
                                while (readerSalesPerson.Read())
                                {
                                    salesPerson = readerSalesPerson["PrcCode"].ToString();
                                }
                            }
                        }
                    }

                    string queryStringCustomerGroup = "CALL SOL_SP_ADDON_SFA_INT_FMS_CUST_GROUP_SO('" + parameter.cardCode + "')";

                    using (var commandCustomerGroup = new HanaCommand(queryStringCustomerGroup, connection))
                    {
                        using (var readerCustomerGroup = commandCustomerGroup.ExecuteReader())
                        {
                            if (readerCustomerGroup.HasRows)
                            {
                                while (readerCustomerGroup.Read())
                                {
                                    customerGroup = readerCustomerGroup["PrcCode"].ToString();
                                }
                            }
                        }
                    }

                    connection.Close();
                }

                SAPbobsCOM.Documents oSales = sboConnection.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

                oSales.CardCode = parameter.cardCode;
                oSales.DocDate = tanggal;
                oSales.DocDueDate = tanggal;
                oSales.SalesPersonCode = parameter.salesCode;
                oSales.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = parameter.sfaRefrenceNumber;
                //oSales.UserFields.Fields.Item("U_SOL_WILAYAH").Value = oRecWilayah.Fields.Item("U_SOL_WILAYAH").Value;
                oSales.UserFields.Fields.Item("U_SOL_WILAYAH").Value = wilayah;

                foreach (var detail in parameter.detail)
                {
                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_FMS_PRODUCT_GROUP_SO('" + detail.itemCode + "')";

                        using (var command = new HanaCommand(queryString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        productGroup = reader["PrcCode"].ToString();
                                    }
                                }
                            }
                        }

                        string queryStringBrand = "CALL SOL_SP_ADDON_SFA_INT_FMS_BRAND_SO('" + detail.itemCode + "')";

                        using (var commandBrand = new HanaCommand(queryStringBrand, connection))
                        {
                            using (var readerBrand = commandBrand.ExecuteReader())
                            {
                                if (readerBrand.HasRows)
                                {
                                    while (readerBrand.Read())
                                    {
                                        brand = readerBrand["PrcCode"].ToString();
                                    }
                                }
                            }
                        }
                        connection.Close();
                    }

                    oSales.Lines.ItemCode = detail.itemCode;
                    oSales.Lines.Quantity = detail.quantity;
                    oSales.Lines.UnitPrice = detail.unitPrice;
                    oSales.Lines.WarehouseCode = detail.warehouseCode;
                    oSales.Lines.CostingCode = detail.kodeCabang;
                    oSales.Lines.CostingCode2 = productGroup;
                    oSales.Lines.CostingCode3 = brand;
                    oSales.Lines.CostingCode4 = salesPerson;
                    oSales.Lines.CostingCode5 = customerGroup;
                    oSales.Lines.UserFields.Fields.Item("U_SOL_HARGA_PRODUK").Value = detail.unitPrice;

                    oSales.Lines.Add();
                }

                int retval = 0;

                retval = oSales.Add();

                if (retval != 0)
                {
                    sboConnection.oCompany.Disconnect();

                    string objectLog = "SO - ADD";
                    string status = "ERROR";
                    string errorMsg = "Create Sales Order Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                    log.insertLog(objectLog, status, errorMsg);

                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                    {
                        responseCode = "500",
                        responseMessage = errorMsg
                    });
                }
                else
                {
                    sboConnection.oCompany.Disconnect();

                    string objectLog = "SO - ADD";
                    string status = "SUCCESS";
                    string errorMsg = "";

                    log.insertLog(objectLog, status, errorMsg);

                    return StatusCode(StatusCodes.Status200OK, new StatusResponse
                    {
                        responseCode = "200",
                        responseMessage = "Sales Order added to SAP."
                    });
                }
            }
            catch (Exception ex)
            {
                sboConnection.connectSBO();

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message,

                });
            }
        }
    }
}
