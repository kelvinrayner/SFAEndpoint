using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;

namespace SFAEndpoint.Controllers
{
    public class StockWarehouseController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public StockWarehouseController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        [HttpPost("/sapapi/sfaintegration/stockwarehouse/master")]
        [Authorize]
        public IActionResult GetStockWarehouse()
        {
            StockWarehouse stockWarehouse = new StockWarehouse();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<StockWarehouse> listStockWarehouse = new List<StockWarehouse>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_STOCK()";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Stock Warehouse not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    stockWarehouse = new StockWarehouse
                                    {
                                        kodeProductPrincipal = reader["kodeProductPrincipal"].ToString(),
                                        deskripsiProduct = reader["deskripsiProduct"].ToString(),
                                        quantity = Convert.ToDouble(reader["quantity"]),
                                        warehouseCode = reader["warehouseCode"].ToString(),
                                        kodeCabang = reader["kodeCabang"].ToString()
                                    };

                                    listStockWarehouse.Add(stockWarehouse);

                                    data = new Data
                                    {
                                        data = listStockWarehouse
                                    };
                                }
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
