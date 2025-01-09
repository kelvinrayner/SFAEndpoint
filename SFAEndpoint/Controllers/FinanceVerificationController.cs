using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

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

        [HttpPost("/sapapi/sfaintegration/financeverification/new")]
        [Authorize]
        public IActionResult PostFinanceVerification([FromBody] FinanceVerificationParameter parameter)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);

            DateTime requestDate = parameter.requestDate.ToDateTime(TimeOnly.MinValue);

            try
            {
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
                oGeneralData.SetProperty("U_SOL_CARD_CODE", parameter.customerCode);
                oGeneralData.SetProperty("U_SOL_CARD_NAME", parameter.customerName);
                oGeneralData.SetProperty("U_SOL_SALES_CODE", parameter.salesCode);
                oGeneralData.SetProperty("U_SOL_SALES_NAME", parameter.salesName);
                oGeneralData.SetProperty("U_SOL_REF_SKA_NUM", parameter.skaRefrenceNumber);
                //oGeneralData.SetProperty("U_SOL_DOC_TOTAL", 100000);
                oGeneralData.SetProperty("U_SOL_REQ_DATE", Convert.ToDateTime(parameter.requestDate));
                oGeneralData.SetProperty("U_SOL_WILAYAH", parameter.wilayah);
                oGeneralData.SetProperty("U_SOL_ACCT_TRF", parameter.accountTransfer);
                oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

                foreach (var detail in parameter.detail)
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
                    oSon.SetProperty("U_SOL_ITEM_CODE", itemCode);
                    oSon.SetProperty("U_SOL_ITEM_NAME", itemName);
                    oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                    oSon.SetProperty("U_SOL_PRICE", Convert.ToDouble(detail.price));
                    oSon.SetProperty("U_SOL_WHS_CODE", detail.warehouseCode);
                }

                //Add records
                oGeneralService.Add(oGeneralData);

                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status200OK, new StatusResponse
                {
                    responseCode = "200",
                    responseMessage = "Finance Verification added to SAP."
                });
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
