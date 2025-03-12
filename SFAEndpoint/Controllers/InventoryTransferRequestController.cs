using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransferRequestController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public InventoryTransferRequestController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/inventorytransferrequest")]
        [Authorize]
        public IActionResult GetITR([FromBody] InventoryTransferRequestParameter parameter)
        {
            Data data = new Data();
            InventoryTransferRequest inventoryTransferRequest = new InventoryTransferRequest();
            InventoryTransferRequestDetail inventoryTransferRequestDetail = new InventoryTransferRequestDetail();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_ITR_HEADER('" + parameter.sfaRefrenceNumber + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Inventory Transfer Request not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    List<InventoryTransferRequestDetail> listInventoryTransferRequestDetail = new List<InventoryTransferRequestDetail>();

                                    int docEntry;
                                    docEntry = Convert.ToInt32(reader["docEntry"]);

                                    string queryStringDetail = "CALL SOL_SP_ADDON_SFA_INT_ITR_DETAIL(" + docEntry + ")";

                                    using (var commandDetail = new HanaCommand(queryStringDetail, connection))
                                    {
                                        using (var readerDetail = commandDetail.ExecuteReader())
                                        {
                                            if (!readerDetail.HasRows)
                                            {
                                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                                {
                                                    responseCode = "404",
                                                    responseMessage = "Inventory Transfer Request Detail not found.",

                                                });
                                            }
                                            else
                                            {
                                                while (readerDetail.Read())
                                                {
                                                    inventoryTransferRequestDetail = new InventoryTransferRequestDetail
                                                    {
                                                        lineNumSAP = Convert.ToInt32(readerDetail["lineNum"]),
                                                        itemCode = readerDetail["itemCode"].ToString(),
                                                        itemName = readerDetail["itemName"].ToString(),
                                                        qty = Convert.ToDouble(readerDetail["quantity"])
                                                    };

                                                    listInventoryTransferRequestDetail.Add(inventoryTransferRequestDetail);
                                                }
                                            }
                                        }
                                    }

                                    inventoryTransferRequest = new InventoryTransferRequest
                                    {
                                        docEntrySAP = docEntry,
                                        docNumSAP = reader["docnumSAP"].ToString(),
                                        docDate = reader["docDate"].ToString(),
                                        salesCode = Convert.ToInt32(reader["salesCode"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNumber"].ToString(),
                                        detail = listInventoryTransferRequestDetail
                                    };
                                }

                                data = new Data
                                {
                                    data = inventoryTransferRequest
                                };
                            }
                        }
                    }
                    connection.Close();
                }
                return Ok(data);
            }
            catch (HanaException hx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = "HANA Error: " + hx.Message,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message,

                });
            }
        }
    }
}
