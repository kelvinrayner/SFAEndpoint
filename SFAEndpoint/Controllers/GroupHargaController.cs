using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupHargaController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public GroupHargaController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/groupharga/header/all")]
        //public IActionResult GetGroupHargaHeader()
        //{
        //    GroupHargaHeader groupHargaHeader = new GroupHargaHeader();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<GroupHargaHeader> listGroupHargaHeader = new List<GroupHargaHeader>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_GRP_HARGA_H(0)";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {

        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Group Harga Header not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            groupHargaHeader = new GroupHargaHeader
        //                            {
        //                                kodeGroupHarga = reader["kodeGroupHarga"].ToString(),
        //                                deskripsi = reader["deskripsi"].ToString()
        //                            };

        //                            listGroupHargaHeader.Add(groupHargaHeader);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listGroupHargaHeader
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

        [HttpPost("/sapapi/sfaintegration/groupharga/header")]
        [Authorize]
        public IActionResult GetSpesificGroupHargaHeader([FromBody] GroupHargaHeaderParameter groupHargaHeaderParameter)
        {
            GroupHargaHeader groupHargaHeader = new GroupHargaHeader();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<GroupHargaHeader> listGroupHargaHeader = new List<GroupHargaHeader>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_GRP_HARGA_H(" + groupHargaHeaderParameter.listNum + ")";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Group Harga Header not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    groupHargaHeader = new GroupHargaHeader
                                    {
                                        kodeGroupHarga = reader["kodeGroupHarga"].ToString(),
                                        deskripsi = reader["deskripsi"].ToString()
                                    };

                                    listGroupHargaHeader.Add(groupHargaHeader);
                                }

                                data = new Data
                                {
                                    data = listGroupHargaHeader
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

        [HttpPost("/sapapi/sfaintegration/groupharga/detail")]
        [Authorize]
        public IActionResult GetGroupHargaDetail()
        {
            GroupHargaDetail groupHargaDetail = new GroupHargaDetail();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<GroupHargaDetail> listGroupHargaDetail = new List<GroupHargaDetail>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_GRP_HARGA_D";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Group Harga Header not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    groupHargaDetail = new GroupHargaDetail
                                    {
                                        kodeGroupHarga = reader["kodeGroupHarga"].ToString(),
                                        kodeProduk = reader["deskripsi"].ToString(),
                                        hargaJualKecil = Convert.ToDecimal(reader["hargaJualKecil"]),
                                        hargaJualTengah = 0,
                                        hargaJualBesar = 0

                                    };

                                    listGroupHargaDetail.Add(groupHargaDetail);
                                }

                                data = new Data
                                {
                                    data = listGroupHargaDetail
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
