using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductLineController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ProductLineController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/productline/master/all")]
        //public IActionResult GetAllProductLine()
        //{
        //    ProductLine productLine = new ProductLine();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<ProductLine> listProductLine = new List<ProductLine>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT_LINE(0)";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {

        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Product Line not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            productLine = new ProductLine
        //                            {
        //                                kodeProductLine = reader["kodeProductLine"].ToString(),
        //                                deskripsi = reader["deskripsi"].ToString(),
        //                                flagKompetitor = reader["flagKompetitor"].ToString()
        //                            };

        //                            listProductLine.Add(productLine);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listProductLine
        //                        };
        //                    }
        //                }
        //            }
        //            connection.Close();
        //        }
        //        return Ok(data);
        //    }
        //    catch (HanaException hx)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
        //        {
        //            responseCode = "500",
        //            responseMessage = "HANA Error: " + hx.Message,

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
        //        {
        //            responseCode = "500",
        //            responseMessage = ex.Message,

        //        });
        //    }
        //}

        [HttpPost("/sapapi/sfaintegration/productline/master")]
        [Authorize]
        public IActionResult GetSpesificProductLine([FromBody] ProductLineParameter productLineParameter)
        {
            ProductLine productLine = new ProductLine();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<ProductLine> listProductLine = new List<ProductLine>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT_LINE(" + productLineParameter.code + ")";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Product Line not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    productLine = new ProductLine
                                    {
                                        kodeProductLine = reader["kodeProductLine"].ToString(),
                                        deskripsi = reader["deskripsi"].ToString(),
                                        flagKompetitor = reader["flagKompetitor"].ToString()
                                    };

                                    listProductLine.Add(productLine);
                                }

                                data = new Data
                                {
                                    data = listProductLine
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
