using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Models;

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

        [HttpPost("/sapapi/sfaintegration/unloadingstock/new")]
        [Authorize]
        public IActionResult PostUnloadingStock([FromBody] InventoryTransferParameter inventoryTransfer)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            try
            {
                SAPbobsCOM.StockTransfer oIT = sboConnection.oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

                oIT.DocDate = DateTime.Now;
                oIT.FromWarehouse = inventoryTransfer.fromWarehouse;
                oIT.ToWarehouse = inventoryTransfer.toWarehouse;
                oIT.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = inventoryTransfer.sfaRefrenceNumber;

                foreach (var detail in inventoryTransfer.detail)
                {
                    oIT.Lines.BaseEntry = inventoryTransfer.docEntrySAP;
                    oIT.Lines.BaseType = SAPbobsCOM.InvBaseDocTypeEnum.InventoryTransferRequest;
                    oIT.Lines.BaseLine = detail.lineNumSAP;
                    oIT.Lines.ItemCode = detail.itemCode;
                    oIT.Lines.Quantity = detail.quantity;

                    oIT.Lines.Add();
                }

                int retval = 0;

                retval = oIT.Add();

                if (retval != 0)
                {
                    sboConnection.oCompany.Disconnect();

                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                    {
                        responseCode = "500",
                        responseMessage = "Create Unloading Stock Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "")
                    });
                }
                else
                {
                    sboConnection.oCompany.Disconnect();

                    return StatusCode(StatusCodes.Status200OK, new StatusResponse
                    {
                        responseCode = "200",
                        responseMessage = "Unloading Stock added to SAP."
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
