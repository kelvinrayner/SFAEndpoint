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

            var connection = new HanaConnection(_connectionStringHana);

            DateTime tanggal = parameter.requestDate.ToDateTime(TimeOnly.MinValue);

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
                oGeneralData.SetProperty("U_SOL_REQ_DATE", tanggal);
                oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

                foreach(var detail in parameter.detail)
                {
                    string itemCode = "";
                    string itemName = "";
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

                        string queryStringWhsCode = "CALL SOL_SP_ADDON_SFA_INT_GET_WHS_BIN_CODE('" + detail.toBinCode + "')";

                        using (var commandWhsCode = new HanaCommand(queryStringWhsCode, connection))
                        {
                            using (var readerWhsCode = commandWhsCode.ExecuteReader())
                            {
                                if (readerWhsCode.HasRows)
                                {
                                    while (readerWhsCode.Read())
                                    {
                                        whsCode = readerWhsCode["WhsCode"].ToString();
                                    }
                                }
                            }
                        }

                        connection.Close();
                    }

                    //Specify data for child UDO
                    oSons = oGeneralData.Child("SOL_D_STOCK_REQ");
                    oSon = oSons.Add();
                    oSon.SetProperty("U_SOL_ITEM_PRINCIPAL", detail.kodeProdukPrincipal);
                    oSon.SetProperty("U_SOL_ITEM_CODE", itemCode);
                    oSon.SetProperty("U_SOL_ITEM_NAME", itemName);
                    oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                    oSon.SetProperty("U_SOL_FROM_WHS", detail.fromWarehouse);
                    oSon.SetProperty("U_SOL_FROM_BIN", detail.fromBinCode);
                    oSon.SetProperty("U_SOL_TO_WHS", whsCode);
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
