using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public ProductController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/product/master/all")]
        //public IActionResult GetAllProduct()
        //{
        //    Product product = new Product();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<Product> listProduct = new List<Product>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT('')";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {

        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Product not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            product = new Product
        //                            {
        //                                kodeProductLine = "",
        //                                kodeProduct = reader["kodeProduct"].ToString(),
        //                                kodeProductPrincipal = "",
        //                                deskripsiProduct = reader["deskripsiProduct"].ToString(),
        //                                uomBesar = "",
        //                                uomTengah = "",
        //                                uomKecil = reader["uomKecil"].ToString(),
        //                                konversiTengah = 0,
        //                                konversiBesar = 0,
        //                                pajak = Convert.ToDecimal(reader["pajak"])
        //                            };

        //                            listProduct.Add(product);

        //                            data = new Data
        //                            {
        //                                data = listProduct
        //                            };
        //                        }
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

        [HttpPost("/sapapi/sfaintegration/product/master")]
        [Authorize]
        public IActionResult GetSpesificProduct()
        {
            Product product = new Product();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<Product> listProduct = new List<Product>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_PRODUCT('')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Product not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    product = new Product
                                    {
                                        kodeProductLine = reader["kodeProductLine"].ToString(),
                                        kodeProduct = reader["kodeProduct"].ToString(),
                                        kodeProductPrincipal = reader["kodeProductPrincipal"].ToString(),
                                        deskripsiProduct = reader["deskripsiProduct"].ToString(),
                                        uomBesar = "",
                                        uomTengah = "",
                                        uomKecil = reader["uomKecil"].ToString(),
                                        konversiTengah = 0,
                                        konversiBesar = 0,
                                        pajak = Convert.ToDecimal(reader["pajak"]),
                                        itemGroupCode = Convert.ToInt32(reader["itemGroupCode"]),
                                        itemGroupName = reader["itemGroupName"].ToString(),
                                        active = reader["active"].ToString(),
                                    };

                                    listProduct.Add(product);

                                    data = new Data
                                    {
                                        data = listProduct
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
