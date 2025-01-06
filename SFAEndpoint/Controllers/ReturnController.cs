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
        public IActionResult PostReturn([FromBody] ReturnParameter parameter)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
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

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_RETUR('" + parameter.sfaRefrenceNumber + "')";

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
                        oReturn.CardCode = parameter.cardCode;
                        oReturn.SalesPersonCode = parameter.salesCode;
                        oReturn.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = parameter.sfaRefrenceNumber;
                        oReturn.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                        //oReturn.DocumentsOwner = EmpId;

                        foreach (var detail in parameter.detail)
                        {
                            int lineNum = 0;
                            string whsCode = "";

                            using (connection)
                            {
                                connection.Open();

                                string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_DELIVERY_DETAIL(" + docEntryDO + ", '" + detail.itemCode + "')";

                                using (var command = new HanaCommand(queryString, connection))
                                {
                                    using (var reader = command.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                lineNum = Convert.ToInt32(reader["lineNumSAP"]);
                                                whsCode = reader["whsCode"].ToString();
                                            }
                                        }
                                    }
                                }
                                connection.Close();
                            }

                            oReturn.Lines.BaseEntry = docEntryDO;
                            oReturn.Lines.BaseType = 15;
                            oReturn.Lines.BaseLine = lineNum;
                            oReturn.Lines.ItemCode = detail.itemCode;
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
                            string errorMsg = "Create Return Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

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

                            string objectLog = "RETURN - ADD";
                            string status = "SUCCESS";
                            string errorMsg = "";

                            log.insertLog(objectLog, status, errorMsg);

                            return StatusCode(StatusCodes.Status200OK, new StatusResponse
                            {
                                responseCode = "200",
                                responseMessage = "Return added to SAP."
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
                else if (doStatus == "C" && doCanceled == "N" && arInvStatus == "O" && arInvCanceled == "N")
                {
                    try
                    {
                        Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                        oCreditMemo.DocDate = parameter.tanggal;
                        oCreditMemo.CardCode = parameter.cardCode;
                        oCreditMemo.SalesPersonCode = parameter.salesCode;
                        oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = parameter.sfaRefrenceNumber;
                        //oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                        //oReturn.DocumentsOwner = EmpId;

                        foreach (var detail in parameter.detail)
                        {
                            int lineNum = 0;
                            string whsCode = "";

                            using (connection)
                            {
                                connection.Open();

                                string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_INVOICE_CM_DETAIL(" + docEntryARInv + ", '" + detail.itemCode + "')";

                                using (var command = new HanaCommand(queryString, connection))
                                {
                                    using (var reader = command.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                lineNum = Convert.ToInt32(reader["lineNumSAP"]);
                                                whsCode = reader["whsCode"].ToString();
                                            }
                                        }
                                    }
                                }
                                connection.Close();
                            }

                            oCreditMemo.Lines.BaseEntry = docEntryARInv;
                            oCreditMemo.Lines.BaseType = 13;
                            oCreditMemo.Lines.BaseLine = lineNum;
                            oCreditMemo.Lines.ItemCode = detail.itemCode;
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
                            string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

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

                            string objectLog = "AR CREDIT MEMO BASED AR INV - ADD";
                            string status = "SUCCESS";
                            string errorMsg = "";

                            log.insertLog(objectLog, status, errorMsg);

                            return StatusCode(StatusCodes.Status200OK, new StatusResponse
                            {
                                responseCode = "200",
                                responseMessage = "AR Credit Memo added to SAP."
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
                else if (arInvStatus == "C" && arInvCanceled == "N" && docNumIncoming != 0)
                {
                    try
                    {
                        string branch = "";
                        string productGroup = "";
                        string brand = "";
                        string salesPerson = "";
                        string customerGroup = "";

                        using (connection)
                        {
                            connection.Open();

                            string queryString = "CALL SOL_SP_ADDON_SFA_INT_FMS_BRANCH_SO(" + parameter.salesCode + ")";

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


                        Documents oCreditMemo = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);

                        oCreditMemo.DocDate = parameter.tanggal;
                        oCreditMemo.CardCode = parameter.cardCode;
                        oCreditMemo.SalesPersonCode = parameter.salesCode;
                        oCreditMemo.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = parameter.sfaRefrenceNumber;
                        //oCreditMemo.UserFields.Fields.Item("U_SOL_DOC_DATE_SFA").Value = parameter.tanggal;
                        //oReturn.DocumentsOwner = EmpId;

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

                            oCreditMemo.Lines.ItemCode = detail.itemCode;
                            oCreditMemo.Lines.Quantity = detail.quantity;
                            oCreditMemo.Lines.WarehouseCode = detail.warehouseCode;
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
                            string errorMsg = "Create AR Credit Memo Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

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

                            string objectLog = "AR CREDIT MEMO - ADD";
                            string status = "SUCCESS";
                            string errorMsg = "";

                            log.insertLog(objectLog, status, errorMsg);

                            return StatusCode(StatusCodes.Status200OK, new StatusResponse
                            {
                                responseCode = "200",
                                responseMessage = "AR Credit Memo added to SAP."
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
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                    {
                        responseCode = "500",
                        responseMessage = "Document Status Ambigious.",

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
