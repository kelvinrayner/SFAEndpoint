using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    public class InventoryTransferController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public InventoryTransferController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/inventorytransfer/new")]
        [Authorize]
        public IActionResult PostInventoryTransfer([FromBody] InventoryTransferParameter inventoryTransfer)
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
                        responseMessage = "Create Inventory Transfer Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "")
                    });
                }
                else
                {
                    sboConnection.oCompany.Disconnect();

                    return StatusCode(StatusCodes.Status200OK, new StatusResponse
                    {
                        responseCode = "200",
                        responseMessage = "Inventory Transfer added to SAP."
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
