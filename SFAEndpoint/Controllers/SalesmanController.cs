using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesmanController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public SalesmanController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        //[HttpPost("/sapapi/sfaintegration/salesman/master/all")]
        //public IActionResult GetAllSalesman()
        //{
        //    Data data = new Data();
        //    Salesman salesman = new Salesman();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<Salesman> listSalesman = new List<Salesman>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_SALESMAN(0)";

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
        //                            salesman = new Salesman
        //                            {
        //                                kodeSalesman = reader["kodeSalesman"].ToString(),
        //                                deskripsi = reader["deskripsi"].ToString(),
        //                                typeOperasiSalesman = "C",
        //                                kodeTeam = "NA",
        //                                kodeGudang = "",
        //                                kodeDistributor = reader["kodeDistributor"].ToString(),
        //                            };

        //                            listSalesman.Add(salesman);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listSalesman
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

        [HttpPost("/sapapi/sfaintegration/salesman/master")]
        [Authorize]
        public IActionResult GetSpesificSalesman([FromBody] SalesmanParameter salesmanParameter)
        {
            Data data = new Data();
            Salesman salesman = new Salesman();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<Salesman> listSalesman = new List<Salesman>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_SALESMAN(" + salesmanParameter.salesmanCode + ")";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Salesman not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    salesman = new Salesman
                                    {
                                        kodeSalesman = reader["kodeSalesman"].ToString(),
                                        deskripsi = reader["deskripsi"].ToString(),
                                        typeOperasiSalesman = "C",
                                        kodeTeam = "NA",
                                        kodeGudang = reader["kodeGudang"].ToString(),
                                        kodeDistributor = reader["kodeDistributor"].ToString(),
                                    };

                                    listSalesman.Add(salesman);
                                }

                                data = new Data
                                {
                                    data = listSalesman
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
