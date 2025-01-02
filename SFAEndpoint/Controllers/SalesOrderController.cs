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
                oSales.DocDate = parameter.tanggal;
                oSales.DocDueDate = parameter.tanggal;
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
