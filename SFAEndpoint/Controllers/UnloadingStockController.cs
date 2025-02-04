using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Models;
using SFAEndpoint.Services;
using Sap.Data.Hana;
using System.Reflection.Metadata;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnloadingStockController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public UnloadingStockController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/unloadingstock/new")]
        [Authorize]
        public IActionResult PostUnloadingStock([FromBody] List<UnloadingStockParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                foreach (var request in requests)
                {
                    int absEntryFrom = 0;

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_BINCODE_SALES(" + request.salesCode + ")";

                        using (var command = new HanaCommand(queryString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        absEntryFrom = Convert.ToInt32(reader["AbsEntry"]);
                                    }
                                }
                            }
                        }

                        connection.Close();
                    }

                    SAPbobsCOM.StockTransfer oIT = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

                    oIT.DocDate = DateTime.Now;
                    oIT.SalesPersonCode = request.salesCode;
                    oIT.FromWarehouse = request.fromWarehouse;
                    oIT.ToWarehouse = request.toWarehouse;
                    oIT.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                    oIT.UserFields.Fields.Item("U_SOL_TIPE_IT").Value = "2";

                    foreach (var detail in request.detail)
                    {
                        string itemCode = "";
                        string itemName = "";

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

                            connection.Close();
                        }

                        //oIT.Lines.BaseEntry = -1;
                        oIT.Lines.BaseType = SAPbobsCOM.InvBaseDocTypeEnum.Empty;
                        //oIT.Lines.BaseLine = detail.lineNumSAP;
                        oIT.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                        oIT.Lines.FromWarehouseCode = request.fromWarehouse;
                        oIT.Lines.WarehouseCode = request.toWarehouse;
                        oIT.Lines.ItemCode = itemCode;
                        oIT.Lines.Quantity = detail.quantity;
                        oIT.Lines.Quantity = detail.quantity;
                        oIT.Lines.FromWarehouseCode = request.fromWarehouse;
                        oIT.Lines.WarehouseCode = request.toWarehouse;

                        oIT.Lines.BinAllocations.SetCurrentLine(0);
                        oIT.Lines.BinAllocations.BinActionType = SAPbobsCOM.BinActionTypeEnum.batFromWarehouse;
                        oIT.Lines.BinAllocations.BinAbsEntry = absEntryFrom;
                        oIT.Lines.BinAllocations.Quantity = detail.quantity;
                        oIT.Lines.BinAllocations.Add();

                        oIT.Lines.Add();
                    }

                    int retval = 0;

                    retval = oIT.Add();

                    if (retval != 0)
                    {
                        sboConnection.oCompany.Disconnect();

                        string objectLog = "UNLOADING STOCK - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Create Unloading Stock Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg);

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                        });
                    }
                    else
                    {
                        string objectLog = "UNLOADING STOCK - ADD";
                        string status = "SUCCESS";
                        string errorMsg = "";

                        log.insertLog(objectLog, status, errorMsg);
                    }
                }

                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Unloading Stock added to SAP."
                });
            }
            catch (Exception ex)
            {
                sboConnection.connectSBO();

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                });
            }
        }
    }
}
