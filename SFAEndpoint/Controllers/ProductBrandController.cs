using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductBrandController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ProductBrandController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/productbrand/master/all")]
        //public IActionResult GetAllProductBrand()
        //{
        //    ProductBrand productBrand = new ProductBrand();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<ProductBrand> listProductBrand = new List<ProductBrand>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT_BRAND(0)";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {

        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Product Brand not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            productBrand = new ProductBrand
        //                            {
        //                                kodeProductBrand = reader["kodeProductBrand"].ToString(),
        //                                deskripsi = reader["deskripsi"].ToString()
        //                            };

        //                            listProductBrand.Add(productBrand);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listProductBrand
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

        [HttpPost("/sapapi/sfaintegration/productbrand/master")]
        [Authorize]
        public IActionResult GetSpesificProductBrand()
        {
            ProductBrand productBrand = new ProductBrand();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<ProductBrand> listProductBrand = new List<ProductBrand>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT_BRAND(0)";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Product Brand not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    productBrand = new ProductBrand
                                    {
                                        kodeProductBrand = reader["kodeProductBrand"].ToString(),
                                        deskripsi = reader["deskripsi"].ToString()
                                    };

                                    listProductBrand.Add(productBrand);
                                }

                                data = new Data
                                {
                                    data = listProductBrand
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
