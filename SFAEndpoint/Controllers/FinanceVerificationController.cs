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

            try
            {
                foreach (var request in requests) 
                {
                    DateTime requestDate = request.requestDate.ToDateTime(TimeOnly.MinValue);

                    //Declare all SAPbobsCOM untuk DI API UDO
                    SAPbobsCOM.GeneralService oGeneralService;
                    SAPbobsCOM.GeneralData oGeneralData;
                    SAPbobsCOM.GeneralDataCollection oSons;
                    SAPbobsCOM.GeneralData oSon;

                    SAPbobsCOM.CompanyService oSTR = null;
                    oSTR = sboConnection.oCompany.GetCompanyService();

                    //Get a handle to the Stock Request UDO
                    oGeneralService = oSTR.GetGeneralService("OFVC");

                    //Specify data for main UDO (Header)
                    oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
                    oGeneralData.SetProperty("U_SOL_CARD_CODE", request.customerCode);
                    oGeneralData.SetProperty("U_SOL_CARD_NAME", request.customerName);
                    oGeneralData.SetProperty("U_SOL_SALES_CODE", request.salesCode);
                    oGeneralData.SetProperty("U_SOL_SALES_NAME", request.salesName);
                    oGeneralData.SetProperty("U_SOL_REF_SKA_NUM", request.skaRefrenceNumber);
                    //oGeneralData.SetProperty("U_SOL_DOC_TOTAL", 100000);
                    oGeneralData.SetProperty("U_SOL_REQ_DATE", requestDate);
                    oGeneralData.SetProperty("U_SOL_WILAYAH", request.wilayah);
                    oGeneralData.SetProperty("U_SOL_ACCT_TRF", request.accountTransfer);
                    oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

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

                        //Specify data for child UDO
                        oSons = oGeneralData.Child("SOL_D_FIN_VERIF");
                        oSon = oSons.Add();
                        oSon.SetProperty("U_SOL_ITEM_PRINCIPAL", detail.kodeProdukPrincipal);
                        oSon.SetProperty("U_SOL_ITEM_CODE", itemCode);
                        oSon.SetProperty("U_SOL_ITEM_NAME", itemName);
                        oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                        oSon.SetProperty("U_SOL_PRICE", Convert.ToDouble(detail.price));
                        oSon.SetProperty("U_SOL_WHS_CODE", detail.warehouseCode);
                    }

                    //Add records
                    oGeneralService.Add(oGeneralData);

                    string objectLog = "FINANCE VERIFICATION - ADD";
                    string status = "SUCCESS";
                    string errorMsg = "";

                    log.insertLog(objectLog, status, errorMsg);
                }

                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Finance Verification added to SAP."
                });
            }
            catch (Exception ex)
            {
                sboConnection.connectSBO();

                string objectLog = "FINANCE VERIFICATION  - ADD";
                string status = "ERROR";
                string errorMsg = "Create Finance Verification Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                log.insertLog(objectLog, status, errorMsg);

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message,

                });
            }
        }
    }
}
