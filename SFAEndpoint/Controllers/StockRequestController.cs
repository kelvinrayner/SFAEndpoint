using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockRequestController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public StockRequestController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/stockrequest/new")]
        [Authorize]
        public IActionResult PostStockRequest([FromBody] StockRequestParameter parameter)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

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
                oGeneralService = oSTR.GetGeneralService("OSTR");

                //Specify data for main UDO (Header)
                oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
                oGeneralData.SetProperty("U_SOL_SALES_CODE", parameter.salesCode);
                oGeneralData.SetProperty("U_SOL_SALES_NAME", parameter.salesName);
                oGeneralData.SetProperty("U_SOL_REF_SKA_NUM", parameter.skaRefrenceNumber);
                oGeneralData.SetProperty("U_SOL_REQ_DATE", Convert.ToDateTime(parameter.requestDate));
                oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

                foreach(var detail in parameter.detail)
                {
                    //Specify data for child UDO
                    oSons = oGeneralData.Child("SOL_D_STOCK_REQ");
                    oSon = oSons.Add();
                    oSon.SetProperty("U_SOL_ITEM_CODE", detail.itemCode);
                    oSon.SetProperty("U_SOL_ITEM_NAME", detail.itemName);
                    oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                    oSon.SetProperty("U_SOL_FROM_WHS", detail.fromWarehouse);
                    oSon.SetProperty("U_SOL_FROM_BIN", detail.fromBinCode);
                    oSon.SetProperty("U_SOL_TO_WHS", detail.toWarehouse);
                    oSon.SetProperty("U_SOL_TO_BIN", detail.toBinCode);
                }

                //Add records
                oGeneralService.Add(oGeneralData);

                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status200OK, new StatusResponse
                {
                    responseCode = "200",
                    responseMessage = "Stock Request added to SAP."
                });
            }
            catch (Exception ex)
            {
                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message,

                });
            }
        }
    }
}
