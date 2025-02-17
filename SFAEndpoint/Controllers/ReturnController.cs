using System.Data.Common;
using System.Reflection.Metadata;
using System.Xml.Linq;
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
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ReturnController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();
        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/return/new")]
        [Authorize]
        public IActionResult PostReturn([FromBody] List<ReturnParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            string itemCode = "";
            string itemName = "";
            string sfaRefNum = "";

            try
            {
                sboConnection.oCompany.StartTransaction();
                foreach (var request in requests)
                {
                    sfaRefNum = request.sfaRefrenceNumber;
                    DateTime tanggal = request.tanggal.ToDateTime(TimeOnly.MinValue);

                    bool retur = false;
                    bool arCreditMemoBasedARInv = false;
                    bool arCreditMemoLepas = false;

                    string doStatus = "";
                    string doCanceled = "";
                    string arInvStatus = "";
                    string arInvCanceled = "";

                    int docEntryDO = 0;
                    int docEntryARInv = 0;
                    int docNumIncoming = 0;

                    DateTime docDateD0 = DateTime.Now;

                    oCreditMemoLines creditMemoLines = new oCreditMemoLines();
                    oReturnLines returnLines = new oReturnLines();

                    List<oCreditMemoLines> listCreditMemoLines = new List<oCreditMemoLines>();
                    List<oReturnLines> listReturnLines = new List<oReturnLines>();

                    using (connection)
                    {
                        connection.Open();

                        foreach (var detail in request.detail)
                        {
                            string queryStringStatusDoc = "CALL SOL_SP_ADDON_SFA_INT_GET_RETUR('" + request.sfaRefrenceNumber + "', '" + detail.kodeProdukPrincipal + "')";

                            using (var commandStatusDoc = new HanaCommand(queryStringStatusDoc, connection))
                            {
                                using (var readerStatusDoc = commandStatusDoc.ExecuteReader())
                                {
                                    if (readerStatusDoc.HasRows)
                                    {
                                        while (readerStatusDoc.Read())
                                        {
                                            docEntryDO = Convert.ToInt32(readerStatusDoc["DocEntryDelivery"]);
                                            docEntryARInv = Convert.ToInt32(readerStatusDoc["DocEntryAR"]);
                                            doStatus = readerStatusDoc["DocStatusDelivery"].ToString();
                                            doCanceled = readerStatusDoc["DOCancel"].ToString();
                                            arInvStatus = readerStatusDoc["DocStatusAR"].ToString();
                                            arInvCanceled = readerStatusDoc["DOCancel"].ToString();
                                            docDateD0 = Convert.ToDateTime(readerStatusDoc["DocDateDO"]);
                                            docNumIncoming = Convert.ToInt32(readerStatusDoc["DocNumIncoming"]);
                                        }
                                    }
                                    else
                                    {
                                        return StatusCode(StatusCodes.Status204NoContent, new StatusResponse
                                        {
                                            responseCode = "204",
                                            responseMessage = "SFA Refrence Number: " + request.sfaRefrenceNumber + " not found."

                                        });
                                    }
                                }
                            }

                            if (arInvStatus == "C" && arInvCanceled == "N" && docNumIncoming != 0)
                            {
                                if (request.tanggalARCM.HasValue)
                                {
                                    try
                                    {
                                        string branch = "";
                                        string productGroup = "";
                                        string brand = "";
                                        string salesPerson = "";
                                        string customerGroup = "";
                                        double lineTotal = 0;
                                        string whsCode = "";
                                        int lineNum = 0;

                                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_FMS_BRANCH_SO(" + request.salesCode + ")";

                                        using (var command = new HanaCommand(queryString, connection))
                                        {
                                            using (var reader = command.ExecuteReader())
                                            {
                                                if (reader.HasRows)
                                                {
                                                    while (reader.Read())
                                                    {
                                                        branch = reader["U_SOL_BRANCH"].ToString();
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

                                        string queryStringItemCode = "CALL SOL_SP_ADDON_SFA_INT_GET_ITEM_CODE('" + detail.kodeProdukPrincipal + "')";

                                        using (var commandItemCode = new HanaCommand(queryStringItemCode, connection))
                                        {
                                            using (var readerItemCode = commandItemCode.ExecuteReader())
                                            {
                                                if (readerItemCode.HasRows)
                                                {
                                                    while (readerItemCode.Read())
                                                    {
                                                        itemCode = readerItemCode["ItemCode"].ToString();
                                                        itemName = readerItemCode["ItemName"].ToString();
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

                                        string queryStringLineTotal = "CALL SOL_SP_ADDON_SFA_INT_GET_LINE_TOTAL_ARINV(" + docEntryARInv + ", '" + itemCode + "')";

                                        using (var commandLineTotal = new HanaCommand(queryStringLineTotal, connection))
                                        {
                                            using (var readerLineTotal = commandLineTotal.ExecuteReader())
                                            {
                                                if (readerLineTotal.HasRows)
                                                {
                                                    while (readerLineTotal.Read())
                                                    {
                                                        lineTotal = Convert.ToDouble(readerLineTotal["LineTotal"]);
                                                    }
                                                }
                                            }
                                        }

                                        string queryStringReturn = "CALL SOL_SP_ADDON_SFA_INT_GET_INVOICE_CM_DETAIL(" + docEntryARInv + ", '" + itemCode + "')";

                                        using (var commandReturn = new HanaCommand(queryStringReturn, connection))
                                        {
                                            using (var readerReturn = commandReturn.ExecuteReader())
                                            {
                                                if (readerReturn.HasRows)
                                                {
                                                    while (readerReturn.Read())
                                                    {
                                                        lineNum = Convert.ToInt32(readerReturn["lineNumSAP"]);
                                                        whsCode = readerReturn["whsCode"].ToString();
                                                    }
                                                }
                                            }
                                        }

                                        creditMemoLines = new oCreditMemoLines
                                        {
                                            kodeProdukPrincipal = detail.kodeProdukPrincipal,
                                            itemCode = itemCode,
                                            quantity = detail.quantity,
                                            warehouseCode = whsCode,
                                            lineTotal = lineTotal,
                                            branch = branch,
                                            productGroup = productGroup,
                                            brand = brand,
                                            salesPerson = salesPerson,
                                            customerGroup = customerGroup,
                                        };

                                        listCreditMemoLines.Add(creditMemoLines);
                                    }
                                    catch (Exception ex)
                                    {
                                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                        {
                                            responseCode = "500",
                                            responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                                        });
                                    }
                                }
                                else
                                {
                                    return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse
                                    {
                                        responseCode = "400",
                                        responseMessage = "Key tanggalARCM is required.",

                                    });
                                }
                            }
                            else if (doStatus == "C" && doCanceled == "N" && arInvStatus == "O" && arInvCanceled == "N")
                            {
                                if (request.tanggalARCM.HasValue)
                                {
                                    try
                                    {
                                        int lineNum = 0;
                                        string whsCode = "";

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

                                        string queryStringReturn = "CALL SOL_SP_ADDON_SFA_INT_GET_INVOICE_CM_DETAIL(" + docEntryARInv + ", '" + itemCode + "')";

                                        using (var commandReturn = new HanaCommand(queryStringReturn, connection))
                                        {
                                            using (var readerReturn = commandReturn.ExecuteReader())
                                            {
                                                if (readerReturn.HasRows)
                                                {
                                                    while (readerReturn.Read())
                                                    {
                                                        lineNum = Convert.ToInt32(readerReturn["lineNumSAP"]);
                                                        whsCode = readerReturn["whsCode"].ToString();
                                                    }
                                                }
                                            }
                                        }

                                        creditMemoLines = new oCreditMemoLines
                                        {
                                            docEntryARInv = docEntryARInv,
                                            lineNum = lineNum,
                                            kodeProdukPrincipal = detail.kodeProdukPrincipal,
                                            itemCode = itemCode,
                                            quantity = detail.quantity,
                                            warehouseCode = whsCode
                                        };

                                        listCreditMemoLines.Add(creditMemoLines);
                                    }
                                    catch (Exception ex)
                                    {
                                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                        {
                                            responseCode = "500",
                                            responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                                        });
                                    }
                                }
                                else
                                {
                                    return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse
                                    {
                                        responseCode = "400",
                                        responseMessage = "Key tanggalARCM is required.",

                                    });
                                }
                            }
                            else if (doStatus == "O" && doCanceled == "N")
                            {
                                try
                                {
                                    int lineNum = 0;
                                    string whsCode = "";

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

                                    string queryStringDelivery = "CALL SOL_SP_ADDON_SFA_INT_GET_DELIVERY_DETAIL(" + docEntryDO + ", '" + itemCode + "')";

                                    using (var commandDelivery = new HanaCommand(queryStringDelivery, connection))
                                    {
                                        using (var readerDelivery = commandDelivery.ExecuteReader())
                                        {
                                            if (readerDelivery.HasRows)
                                            {
                                                while (readerDelivery.Read())
                                                {
                                                    lineNum = Convert.ToInt32(readerDelivery["lineNumSAP"]);
                                                    whsCode = readerDelivery["whsCode"].ToString();
                                                }
                                            }
                                        }
                                    }

                                    returnLines = new oReturnLines
                                    {
                                        docEntryDO = docEntryDO,
                                        lineNum = lineNum,
                                        kodeProdukPrincipal = detail.kodeProdukPrincipal,
                                        itemCode = itemCode,
                                        quantity = detail.quantity,
                                        warehouseCode = whsCode
                                    };

                                    listReturnLines.Add(returnLines);
                                }
                                catch (Exception ex)
                                {
                                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                    {
                                        responseCode = "500",
                                        responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                                    });
                                }
                            }
                            else
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = "Document Status Ambigious.",

                                });
                            }
                        }
                        connection.Close();
                    }

                    if (arInvStatus == "C" && arInvCanceled == "N" && docNumIncoming != 0)
                    {
                        try
                        {
                            Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                            DateTime tanggalARCM = request.tanggalARCM.Value.ToDateTime(TimeOnly.MinValue);

                            oCreditMemo.DocDate = tanggalARCM;
                            oCreditMemo.CardCode = request.cardCode;
                            oCreditMemo.SalesPersonCode = request.salesCode;
                            oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                            oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = request.tanggalARCM;
                            //oReturn.DocumentsOwner = EmpId;

                            foreach (var detail in listCreditMemoLines)
                            {
                                oCreditMemo.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                oCreditMemo.Lines.ItemCode = detail.itemCode;
                                oCreditMemo.Lines.Quantity = detail.quantity;
                                oCreditMemo.Lines.WarehouseCode = detail.warehouseCode;
                                oCreditMemo.Lines.LineTotal = detail.lineTotal;
                                oCreditMemo.Lines.CostingCode = detail.branch;
                                oCreditMemo.Lines.CostingCode2 = detail.productGroup;
                                oCreditMemo.Lines.CostingCode3 = detail.brand;
                                oCreditMemo.Lines.CostingCode4 = detail.salesPerson;
                                oCreditMemo.Lines.CostingCode5 = detail.customerGroup;

                                oCreditMemo.Lines.Add();
                            }

                            int retval = 0;

                            retval = oCreditMemo.Add();

                            if (retval != 0)
                            {
                                sboConnection.oCompany.Disconnect();

                                string objectLog = "AR CREDIT MEMO - ADD";
                                string status = "ERROR";
                                string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                                string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                                });
                            }
                            else
                            {
                                string objectLog = "AR CREDIT MEMO - ADD";
                                string status = "SUCCESS";
                                string errorMsg = "";

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            sboConnection.connectSBO();

                            string objectLog = "AR CREDIT MEMO - ADD";
                            string status = "ERROR";
                            string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                            log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                            sboConnection.oCompany.Disconnect();

                            return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                            {
                                responseCode = "500",
                                responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                            });
                        }

                    }
                    else if (doStatus == "C" && doCanceled == "N" && arInvStatus == "O" && arInvCanceled == "N")
                    {
                        try
                        {
                            DateTime tanggalARCM = request.tanggalARCM.Value.ToDateTime(TimeOnly.MinValue);

                            Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                            oCreditMemo.DocDate = tanggalARCM;
                            oCreditMemo.CardCode = request.cardCode;
                            oCreditMemo.SalesPersonCode = request.salesCode;
                            oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                            //oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                            //oReturn.DocumentsOwner = EmpId;

                            foreach (var detail in listCreditMemoLines)
                            {
                                oCreditMemo.Lines.BaseEntry = detail.docEntryARInv;
                                oCreditMemo.Lines.BaseType = 13;
                                oCreditMemo.Lines.BaseLine = detail.lineNum;
                                oCreditMemo.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                oCreditMemo.Lines.ItemCode = detail.itemCode;
                                oCreditMemo.Lines.Quantity = detail.quantity;
                                oCreditMemo.Lines.WarehouseCode = detail.warehouseCode;

                                oCreditMemo.Lines.Add();
                            }

                            int retval = 0;

                            retval = oCreditMemo.Add();

                            if (retval != 0)
                            {
                                sboConnection.oCompany.Disconnect();

                                string objectLog = "AR CREDIT MEMO BASED AR INV - ADD";
                                string status = "ERROR";
                                string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                                string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                                });
                            }
                            else
                            {
                                string objectLog = "AR CREDIT MEMO BASED AR INV - ADD";
                                string status = "SUCCESS";
                                string errorMsg = "";

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            sboConnection.connectSBO();

                            string objectLog = "AR CREDIT MEMO - ADD";
                            string status = "ERROR";
                            string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                            log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                            sboConnection.oCompany.Disconnect();

                            return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                            {
                                responseCode = "500",
                                responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                            });
                        }
                    }
                    else if (doStatus == "O" && doCanceled == "N")
                    {
                        try
                        {
                            Documents oReturn = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oReturns);

                            oReturn.DocDate = docDateD0;
                            oReturn.CardCode = request.cardCode;
                            oReturn.SalesPersonCode = request.salesCode;
                            oReturn.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                            oReturn.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = request.tanggal.ToDateTime(TimeOnly.MinValue);
                            //oReturn.DocumentsOwner = EmpId;

                            foreach (var detail in listReturnLines)
                            {
                                oReturn.Lines.BaseEntry = detail.docEntryDO;
                                oReturn.Lines.BaseType = 15;
                                oReturn.Lines.BaseLine = detail.lineNum;
                                oReturn.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                oReturn.Lines.ItemCode = detail.itemCode;
                                oReturn.Lines.Quantity = detail.quantity;
                                oReturn.Lines.WarehouseCode = detail.warehouseCode;

                                oReturn.Lines.Add();
                            }

                            int retval = 0;

                            retval = oReturn.Add();

                            if (retval != 0)
                            {
                                sboConnection.oCompany.Disconnect();

                                string objectLog = "RETURN - ADD";
                                string status = "ERROR";
                                string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                                string errorMsg = "Create Return Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                                });
                            }
                            else
                            {
                                string objectLog = "RETURN - ADD";
                                string status = "SUCCESS";
                                string errorMsg = "";

                                log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            sboConnection.connectSBO();

                            string objectLog = "RETURN - ADD";
                            string status = "ERROR";
                            string errorMsg = "Create Return Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                            log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                            sboConnection.oCompany.Disconnect();

                            return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                            {
                                responseCode = "500",
                                responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                            });
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = "Document Status Ambigious.",

                        });
                    }
                }
                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Return / AR Credit Memo added to SAP."
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
