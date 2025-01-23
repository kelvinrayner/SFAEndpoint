using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Models;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnExistingController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ReturnExistingController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();
        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/return-existing/new")]
        [Authorize]
        public IActionResult PostReturn([FromBody] List<ReturnExistingParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            string itemCode = "";
            string itemName = "";

            try
            {
                foreach (var request in requests)
                {
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

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_RETUR('" + request.docnumDeliveryOrder + "')";

                        using (var command = new HanaCommand(queryString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        docEntryDO = Convert.ToInt32(reader["DocEntryDelivery"]);
                                        docEntryARInv = Convert.ToInt32(reader["DocEntryAR"]);
                                        doStatus = reader["DocStatusDelivery"].ToString();
                                        doCanceled = reader["DOCancel"].ToString();
                                        arInvStatus = reader["DocStatusAR"].ToString();
                                        arInvCanceled = reader["DOCancel"].ToString();
                                        docDateD0 = Convert.ToDateTime(reader["DocDateDO"]);
                                        docNumIncoming = Convert.ToInt32(reader["DocNumIncoming"]);
                                    }
                                }
                                else
                                {
                                    return StatusCode(StatusCodes.Status204NoContent, new StatusResponse
                                    {
                                        responseCode = "204",
                                        responseMessage = "SFA Refrence Number: " + request.docnumDeliveryOrder + " not found."

                                    });
                                }
                            }
                        }
                        connection.Close();
                    }

                    if (doStatus == "O" && doCanceled == "N")
                    {
                        try
                        {
                            Documents oReturn = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oReturns);

                            oReturn.DocDate = docDateD0;
                            oReturn.CardCode = request.cardCode;
                            oReturn.SalesPersonCode = request.salesCode;
                            //oReturn.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.docnumDeliveryOrder;
                            oReturn.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = request.tanggal;
                            //oReturn.DocumentsOwner = EmpId;

                            foreach (var detail in request.detail)
                            {
                                int lineNum = 0;
                                string whsCode = "";

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
                                    connection.Close();
                                }

                                oReturn.Lines.BaseEntry = docEntryDO;
                                oReturn.Lines.BaseType = 15;
                                oReturn.Lines.BaseLine = lineNum;
                                oReturn.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                oReturn.Lines.ItemCode = itemCode;
                                oReturn.Lines.Quantity = detail.quantity;
                                oReturn.Lines.WarehouseCode = whsCode;

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

                                log.insertLog(objectLog, status, errorMsg);

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = errorResponse.Substring(0, 255)
                                });
                            }
                            else
                            {
                                string objectLog = "RETURN - ADD";
                                string status = "SUCCESS";
                                string errorMsg = "";

                                log.insertLog(objectLog, status, errorMsg);
                            }
                        }
                        catch (Exception ex)
                        {
                            sboConnection.connectSBO();

                            return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                            {
                                responseCode = "500",
                                responseMessage = ex.Message.Substring(0, 255),

                            });
                        }
                    }
                    else if (doStatus == "C" && doCanceled == "N" && arInvStatus == "O" && arInvCanceled == "N")
                    {
                        if (request.tanggalARCM.HasValue)
                        {
                            DateTime tanggalARCM = request.tanggalARCM.Value.ToDateTime(TimeOnly.MinValue);

                            try
                            {
                                Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                                oCreditMemo.DocDate = tanggalARCM;
                                oCreditMemo.CardCode = request.cardCode;
                                oCreditMemo.SalesPersonCode = request.salesCode;
                                //oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                                //oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                                //oReturn.DocumentsOwner = EmpId;

                                foreach (var detail in request.detail)
                                {
                                    int lineNum = 0;
                                    string whsCode = "";

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
                                        connection.Close();
                                    }

                                    oCreditMemo.Lines.BaseEntry = docEntryARInv;
                                    oCreditMemo.Lines.BaseType = 13;
                                    oCreditMemo.Lines.BaseLine = lineNum;
                                    oCreditMemo.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                    oCreditMemo.Lines.ItemCode = itemCode;
                                    oCreditMemo.Lines.Quantity = detail.quantity;
                                    oCreditMemo.Lines.WarehouseCode = whsCode;

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

                                    log.insertLog(objectLog, status, errorMsg);

                                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                    {
                                        responseCode = "500",
                                        responseMessage = errorResponse.Substring(0, 255)
                                    });
                                }
                                else
                                {
                                    string objectLog = "AR CREDIT MEMO BASED AR INV - ADD";
                                    string status = "SUCCESS";
                                    string errorMsg = "";

                                    log.insertLog(objectLog, status, errorMsg);
                                }
                            }
                            catch (Exception ex)
                            {
                                sboConnection.connectSBO();

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = ex.Message.Substring(0, 255),

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
                    else if (arInvStatus == "C" && arInvCanceled == "N" && docNumIncoming != 0)
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

                                DateTime tanggalARCM = request.tanggalARCM.Value.ToDateTime(TimeOnly.MinValue);

                                using (connection)
                                {
                                    connection.Open();

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

                                    connection.Close();
                                }


                                Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                                oCreditMemo.DocDate = tanggalARCM;
                                oCreditMemo.CardCode = request.cardCode;
                                oCreditMemo.SalesPersonCode = request.salesCode;
                                //oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;
                                //oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                                //oReturn.DocumentsOwner = EmpId;

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
                                        connection.Close();
                                    }

                                    oCreditMemo.Lines.UserFields.Fields.Item("U_SOL_ITEM_PRINCIPAL").Value = detail.kodeProdukPrincipal;
                                    oCreditMemo.Lines.ItemCode = itemCode;
                                    oCreditMemo.Lines.Quantity = detail.quantity;
                                    oCreditMemo.Lines.WarehouseCode = detail.warehouseCode;
                                    oCreditMemo.Lines.LineTotal = lineTotal;
                                    oCreditMemo.Lines.CostingCode = branch;
                                    oCreditMemo.Lines.CostingCode2 = productGroup;
                                    oCreditMemo.Lines.CostingCode3 = brand;
                                    oCreditMemo.Lines.CostingCode4 = salesPerson;
                                    oCreditMemo.Lines.CostingCode5 = customerGroup;

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

                                    log.insertLog(objectLog, status, errorMsg);

                                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                    {
                                        responseCode = "500",
                                        responseMessage = errorResponse.Substring(0, 255)
                                    });
                                }
                                else
                                {
                                    string objectLog = "AR CREDIT MEMO - ADD";
                                    string status = "SUCCESS";
                                    string errorMsg = "";

                                    log.insertLog(objectLog, status, errorMsg);
                                }
                            }
                            catch (Exception ex)
                            {
                                sboConnection.connectSBO();

                                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                                {
                                    responseCode = "500",
                                    responseMessage = ex.Message.Substring(0, 255),

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
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = "Document Status Ambigious.",

                        });
                    }
                }

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
                    responseMessage = ex.Message.Substring(0, 255),

                });
            }
        }
    }
}
