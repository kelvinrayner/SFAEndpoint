using System;
using System.Data.Common;
using System.Reflection.Metadata;
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
                                        customerRefNumSAP = reader["customerRefNum"].ToString(),
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
        public IActionResult PostSalesOrder([FromBody] List<SalesOrderParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            string itemCode = "";
            string itemName = "";
            string wilayah = "";
            string productGroup = "";
            string brand = "";
            string salesPerson = "";
            string customerGroup = "";
            string whsCode = "";
            string kodeCabang = "";
            string sfaRefNum = "";
            List<InsertLog> listLog = new List<InsertLog>();
            SAPbobsCOM.Documents oSales = sboConnection.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

            try
            {
                var connection = new HanaConnection(_connectionStringHana);

                sboConnection.oCompany.StartTransaction();

                foreach (var request in requests)
                {
                    DateTime tanggal = request.tanggal.ToDateTime(TimeOnly.MinValue);

                    sfaRefNum = request.sfaRefrenceNumber;

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_FMS_WILAYAH_SO(" + request.salesCode + ")";

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

                        string queryStringSalesPerson = "CALL SOL_SP_ADDON_SFA_INT_FMS_SALES_PERSON_SO('" + request.salesCode + "')";

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

                        string queryStringCustomerGroup = "CALL SOL_SP_ADDON_SFA_INT_FMS_CUST_GROUP_SO('" + request.cardCode + "')";

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

                    oSales.CardCode = request.cardCode;
                    oSales.DocDate = tanggal;
                    oSales.DocDueDate = tanggal;
                    oSales.SalesPersonCode = request.salesCode;
                    oSales.NumAtCard = request.customerRefNo;
                    oSales.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                    //oSales.UserFields.Fields.Item("U_SOL_WILAYAH").Value = oRecWilayah.Fields.Item("U_SOL_WILAYAH").Value;
                    oSales.UserFields.Fields.Item("U_SOL_WILAYAH").Value = wilayah;

                    foreach (var detail in request.detail)
                    {
                        using (connection)
                        {
                            connection.Open();

                            string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_ITEM_CODE('" + detail.kodeProdukPrincipal + "')";

                            using (var command = new HanaCommand(queryString, connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            itemCode = reader["ItemCode"].ToString();
                                            itemName = reader["ItemName"].ToString();
                                        }
                                    }
                                }
                            }

                            string queryStringGroupSO = "CALL SOL_SP_ADDON_SFA_INT_FMS_PRODUCT_GROUP_SO('" + itemCode + "')";

                            using (var commandGroupSO = new HanaCommand(queryStringGroupSO, connection))
                            {
                                using (var readerGroupSO = commandGroupSO.ExecuteReader())
                                {
                                    if (readerGroupSO.HasRows)
                                    {
                                        while (readerGroupSO.Read())
                                        {
                                            productGroup = readerGroupSO["PrcCode"].ToString();
                                        }
                                    }
                                }
                            }

                            string queryStringBrand = "CALL SOL_SP_ADDON_SFA_INT_FMS_BRAND_SO('" + itemCode + "')";

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

                            string queryStringWhsCode = "CALL SOL_SP_ADDON_SFA_INT_WHS_CODE_BASED_WILAYAH('" + request.salesCode + "')";

                            using (var commandWhsCode = new HanaCommand(queryStringWhsCode, connection))
                            {
                                using (var readerWhsCode = commandWhsCode.ExecuteReader())
                                {
                                    if (readerWhsCode.HasRows)
                                    {
                                        while (readerWhsCode.Read())
                                        {
                                            whsCode = readerWhsCode["WhsCode"].ToString();
                                        }
                                    }
                                }
                            }

                            string queryStringCabangSales = "CALL SOL_SP_ADDON_SFA_INT_CABANG_SALES('" + request.salesCode + "')";

                            using (var commandCabangSales = new HanaCommand(queryStringCabangSales, connection))
                            {
                                using (var readerCabangSales = commandCabangSales.ExecuteReader())
                                {
                                    if (readerCabangSales.HasRows)
                                    {
                                        while (readerCabangSales.Read())
                                        {
                                            kodeCabang = readerCabangSales["U_SOL_BRANCH"].ToString();
                                        }
                                    }
                                }
                            }

                            connection.Close();
                        }

                        oSales.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                        oSales.Lines.ItemCode = itemCode;
                        oSales.Lines.Quantity = detail.quantity;
                        oSales.Lines.UnitPrice = detail.unitPrice;
                        oSales.Lines.WarehouseCode = whsCode;
                        oSales.Lines.CostingCode = kodeCabang;
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
                        string objectLog = "SO - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Create Sales Order Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                        if (oSales != null)
                        {
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSales);
                            oSales = null;
                        }

                        if (sboConnection.oCompany != null)
                        {
                            if (sboConnection.oCompany.Connected)
                            {
                                sboConnection.oCompany.Disconnect();
                            }
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                            sboConnection.oCompany = null;
                        }

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                        });
                    }
                    else
                    {
                        var logData = new InsertLog
                        {
                            objectLog = "SO - ADD",
                            status = "SUCCESS",
                            errorMessage = "",
                            sfaRefNumber = sfaRefNum
                        };

                        listLog.Add(logData);
                    }
                }

                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                foreach (var dataLog in listLog)
                {
                    log.insertLog(dataLog.objectLog, dataLog.status, dataLog.errorMessage, dataLog.sfaRefNumber);
                }

                if (oSales != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSales);
                    oSales = null;
                }

                if (sboConnection.oCompany != null)
                {
                    if (sboConnection.oCompany.Connected)
                    {
                        sboConnection.oCompany.Disconnect();
                    }
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                    sboConnection.oCompany = null;
                }

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Sales Order added to SAP."
                });
            }
            catch (Exception ex)
            {
                if (sboConnection.oCompany.InTransaction)
                {
                    sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }

                string objectLog = "SO - ADD";
                string status = "ERROR";
                string errorMsg = "Create Sales Order Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                if (oSales != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSales);
                    oSales = null;
                }

                if (sboConnection.oCompany != null)
                {
                    if (sboConnection.oCompany.Connected)
                    {
                        sboConnection.oCompany.Disconnect();
                    }
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                    sboConnection.oCompany = null;
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                });
            }
        }
    }
}
