using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceVerificationController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public FinanceVerificationController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/financeverification/new")]
        [Authorize]
        public IActionResult PostFinanceVerification([FromBody] List<FinanceVerificationParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);
            List<InsertLog> listLog = new List<InsertLog>();

            string sfaRefNum = "";

            //Declare all SAPbobsCOM untuk DI API UDO
            SAPbobsCOM.GeneralService oGeneralService = null;
            SAPbobsCOM.GeneralData oGeneralData = null;
            SAPbobsCOM.GeneralDataCollection oSons = null;
            SAPbobsCOM.GeneralData oSon = null; ;

            SAPbobsCOM.CompanyService oSTR = null;
            oSTR = sboConnection.oCompany.GetCompanyService();

            //Get a handle to the Stock Request UDO
            oGeneralService = oSTR.GetGeneralService("OFVC");

            //Specify data for main UDO (Header)
            oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);

            try
            {
                sboConnection.oCompany.StartTransaction();

                foreach (var request in requests) 
                {
                    sfaRefNum = request.skaRefrenceNumber;

                    DateTime requestDate = request.requestDate.ToDateTime(TimeOnly.MinValue);

                    oGeneralData.SetProperty("U_SOL_CARD_CODE", request.customerCode);
                    oGeneralData.SetProperty("U_SOL_CARD_NAME", request.customerName);
                    oGeneralData.SetProperty("U_SOL_SALES_CODE", request.salesCode);
                    oGeneralData.SetProperty("U_SOL_SALES_NAME", request.salesName);
                    oGeneralData.SetProperty("U_SOL_REF_SKA_NUM", sfaRefNum);
                    //oGeneralData.SetProperty("U_SOL_DOC_TOTAL", 100000);
                    oGeneralData.SetProperty("U_SOL_REQ_DATE", requestDate);

                    string wilayah = "";
                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_WILAYAH_SALES('" + request.salesCode + "')";

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

                        connection.Close();
                    }

                    oGeneralData.SetProperty("U_SOL_WILAYAH", wilayah);
                    oGeneralData.SetProperty("U_SOL_ACCT_TRF", request.accountTransfer);
                    oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

                    foreach (var detail in request.detail)
                    {
                        string itemCode = "";
                        string itemName = "";

                        decimal price = Convert.ToDecimal(detail.price) * (100 - Convert.ToDecimal(detail.pajak)) / 100;

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

                        //Specify data for child UDO
                        oSons = oGeneralData.Child("SOL_D_FIN_VERIF");
                        oSon = oSons.Add();
                        oSon.SetProperty("U_SOL_ITEM_PRINCIPAL", detail.kodeProdukPrincipal);
                        oSon.SetProperty("U_SOL_ITEM_CODE", itemCode);
                        oSon.SetProperty("U_SOL_ITEM_NAME", itemName);
                        oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                        oSon.SetProperty("U_SOL_PRICE", Convert.ToDouble(price));
                        oSon.SetProperty("U_SOL_WHS_CODE", detail.warehouseCode);
                    }

                    //Add records
                    oGeneralService.Add(oGeneralData);

                    var logData = new InsertLog
                    {
                        objectLog = "FINANCE VERIFICATION - ADD",
                        status = "SUCCESS",
                        errorMessage = "",
                        sfaRefNumber = sfaRefNum
                    };

                    listLog.Add(logData);
                }
                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                foreach (var dataLog in listLog)
                {
                    log.insertLog(dataLog.objectLog, dataLog.status, dataLog.errorMessage, dataLog.sfaRefNumber);
                }

                if (oGeneralData != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralData);
                    oGeneralData = null;
                }

                if (oGeneralService != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralService);
                    oGeneralService = null;
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
                    responseMessage = "Finance Verification added to SAP."
                });
            }
            catch (Exception ex)
            {
                if (sboConnection.oCompany.InTransaction)
                {
                    sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }

                string objectLog = "FINANCE VERIFICATION  - ADD";
                string status = "ERROR";
                string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                string errorMsg = "Create Finance Verification Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                if (oGeneralData != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralData);
                    oGeneralData = null;
                }

                if (oGeneralService != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralService);
                    oGeneralService = null;
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

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,

                });
            }
        }
    }
}
