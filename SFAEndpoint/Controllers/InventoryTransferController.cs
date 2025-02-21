using System.Data.Common;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    public class InventoryTransferController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public InventoryTransferController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/inventorytransfer/new")]
        [Authorize]
        public IActionResult PostInventoryTransfer([FromBody] List<InventoryTransferParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            string sfaRefNum = "";

            SAPbobsCOM.StockTransfer oIT = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

            try
            {
                sboConnection.oCompany.StartTransaction();

                foreach (var request in requests)
                {
                    int absEntryFrom = 0;
                    int absEntryTo = 0;
                    string fromWhsCode = "";
                    string toWhsCode = "";
                    sfaRefNum = request.sfaRefrenceNumber;

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_ABSENTRY_FROM_BINCODE(" + request.docEntrySAP + ")";

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

                        string queryStringTo = "CALL SOL_SP_ADDON_SFA_INT_GET_ABSENTRY_TO_BINCODE(" + request.docEntrySAP + ")";

                        using (var commandTo = new HanaCommand(queryStringTo, connection))
                        {
                            using (var readerTo = commandTo.ExecuteReader())
                            {
                                if (readerTo.HasRows)
                                {
                                    while (readerTo.Read())
                                    {
                                        absEntryTo = Convert.ToInt32(readerTo["AbsEntry"]);
                                    }
                                }
                            }
                        }

                        string queryStringWhsCode = "CALL SOL_SP_ADDON_SFA_INT_GET_ITR_WHS_CODE(" + request.docEntrySAP + ")";

                        using (var commandWhsCode = new HanaCommand(queryStringWhsCode, connection))
                        {
                            using (var readerWhsCode = commandWhsCode.ExecuteReader())
                            {
                                if (readerWhsCode.HasRows)
                                {
                                    while (readerWhsCode.Read())
                                    {
                                        fromWhsCode = readerWhsCode["fromWhsCode"].ToString();
                                        toWhsCode = readerWhsCode["ToWhsCode"].ToString();
                                    }
                                }
                            }
                        }

                        connection.Close();
                    }

                    oIT.DocDate = DateTime.Now;
                    oIT.SalesPersonCode = request.salesCode;
                    oIT.FromWarehouse = fromWhsCode;
                    oIT.ToWarehouse = toWhsCode;
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

                        oIT.Lines.BaseEntry = request.docEntrySAP;
                        oIT.Lines.BaseType = SAPbobsCOM.InvBaseDocTypeEnum.InventoryTransferRequest;
                        oIT.Lines.BaseLine = detail.lineNumSAP;
                        oIT.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                        oIT.Lines.ItemCode = itemCode;
                        oIT.Lines.Quantity = detail.quantity;
                        oIT.Lines.FromWarehouseCode = fromWhsCode;
                        oIT.Lines.WarehouseCode = toWhsCode;

                        //oIT.Lines.BinAllocations.SetCurrentLine(0);
                        oIT.Lines.BinAllocations.BinActionType = SAPbobsCOM.BinActionTypeEnum.batFromWarehouse;
                        oIT.Lines.BinAllocations.BinAbsEntry = absEntryFrom;
                        oIT.Lines.BinAllocations.Quantity = detail.quantity;
                        oIT.Lines.BinAllocations.Add();

                        //oIT.Lines.BinAllocations.SetCurrentLine(1);
                        oIT.Lines.BinAllocations.BinActionType = SAPbobsCOM.BinActionTypeEnum.batToWarehouse;
                        oIT.Lines.BinAllocations.BinAbsEntry = absEntryTo;
                        oIT.Lines.BinAllocations.Quantity = detail.quantity;
                        oIT.Lines.BinAllocations.Add();

                        oIT.Lines.Add();
                    }

                    int retval = 0;

                    retval = oIT.Add();

                    if (retval != 0)
                    {
                        string objectLog = "IT - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Create Inventory Transfer Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                        if (oIT != null)
                        {
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIT);
                            oIT = null;
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
                        string objectLog = "IT - ADD";
                        string status = "SUCCESS";
                        string errorMsg = "";

                        log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);
                    }
                }
                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                if (oIT != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIT);
                    oIT = null;
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
                    responseMessage = "Inventory Transfer added to SAP."
                });
            }
            catch (Exception ex)
            {
                if (sboConnection.oCompany.InTransaction)
                {
                    sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }

                string objectLog = "IT - ADD";
                string status = "ERROR";
                string errorMsg = "Create Inventory Transfer Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                if (oIT != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIT);
                    oIT = null;
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
