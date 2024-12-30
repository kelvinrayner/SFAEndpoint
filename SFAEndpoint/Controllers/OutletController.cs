using System.Data.Common;
using System.Data.SqlClient;
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
    public class OutletController : ControllerBase
    {
        private readonly string _connectionStringHana;
        private readonly string _connectionStringSqlServer;

        public OutletController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
            _connectionStringSqlServer = configuration.GetConnectionString("SqlServerConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/outlet/master/all")]
        //public IActionResult GetAllOutlet()
        //{
        //    Outlet outlet = new Outlet();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<Outlet> listOutlet = new List<Outlet>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_OUTLET('')";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Outlet not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            outlet = new Outlet
        //                            {
        //                                kodePelanggan = reader["kodePelanggan"].ToString(),
        //                                namaPelanggan = reader["namaPelanggan"].ToString(),
        //                                alamatPelanggan = reader["alamatPelanggan"].ToString(),
        //                                kodeTermOfPayment = reader["kodeTermOfPayment"].ToString(),
        //                                kodeTypeOutlet = reader["kodeTypeOutlet"].ToString(),
        //                                kodeGroupOutlet = "NA",
        //                                kodeGroupHarga = reader["kodeGroupHarga"].ToString(),
        //                                defaultTypePembayaran = reader["defaultTypePembayaran"].ToString(),
        //                                flagOutletRegister = reader["flagOutletRegister"].ToString(),
        //                                kodeDistributor = reader["kodeDistributor"].ToString(),
        //                            };

        //                            listOutlet.Add(outlet);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listOutlet
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

        [HttpPost("/sapapi/sfaintegration/outlet/master")]
        [Authorize]
        public IActionResult GetSpesificOutlet()
        {
            Outlet outlet = new Outlet();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<Outlet> listOutlet = new List<Outlet>();
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_OUTLET('')";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Outlet not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    outlet = new Outlet
                                    {
                                        kodePelanggan = reader["kodePelanggan"].ToString(),
                                        namaPelanggan = reader["namaPelanggan"].ToString(),
                                        alamatPelanggan = reader["alamatPelanggan"].ToString(),
                                        kodeTermOfPayment = reader["kodeTermOfPayment"].ToString(),
                                        kodeTypeOutlet = reader["kodeTypeOutlet"].ToString(),
                                        kodeGroupOutlet = "NA",
                                        kodeGroupHarga = reader["kodeGroupHarga"].ToString(),
                                        defaultTypePembayaran = reader["defaultTypePembayaran"].ToString(),
                                        flagOutletRegister = reader["flagOutletRegister"].ToString(),
                                        kodeDistributor = reader["kodeDistributor"].ToString(),
                                    };

                                    listOutlet.Add(outlet);
                                }

                                data = new Data
                                {
                                    data = listOutlet
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

        //[HttpPost("/sapapi/sfaintegration/typeoutlet/master/all")]
        //public IActionResult GetAllOutletType()
        //{
        //    OutletType outletType = new OutletType();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<OutletType> listOutletType = new List<OutletType>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_TYPE_OUTLET(0)";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Type Outlet not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            outletType = new OutletType
        //                            {
        //                                kodeTypeOutlet = reader["kodeTypeOutlet"].ToString(),
        //                                deskripsi = reader["deskripsi"].ToString()
        //                            };

        //                            listOutletType.Add(outletType);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listOutletType
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

        [HttpPost("/sapapi/sfaintegration/typeoutlet/master")]
        [Authorize]
        public IActionResult GetSpesificOutletType()
        {
            OutletType outletType = new OutletType();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<OutletType> listOutletType = new List<OutletType>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_TYPE_OUTLET(0)";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Type Outlet not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    outletType = new OutletType
                                    {
                                        kodeTypeOutlet = reader["kodeTypeOutlet"].ToString(),
                                        deskripsi = reader["deskripsi"].ToString()
                                    };

                                    listOutletType.Add(outletType);
                                }

                                data = new Data
                                {
                                    data = listOutletType
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

        [HttpPost("/sapapi/sfaintegration/outlet/new")]
        [Authorize]
        public IActionResult PostOutlet([FromBody] Outlet parameter)
        {
            SBOConnection sboConnection = new SBOConnection();
            sboConnection.connectSBO();

            var connectionSqlServer = new SqlConnection(_connectionStringSqlServer);

            try
            {
                if (parameter.flagOutletRegister == "C")
                {
                    parameter.flagOutletRegister = "Y";
                }

                if (parameter.defaultTypePembayaran == "K")
                {
                    parameter.defaultTypePembayaran = "Kredit";
                }
                else if(parameter.defaultTypePembayaran == "T")
                {
                    parameter.defaultTypePembayaran = "Tunai";
                }

                SAPbobsCOM.Company oCompany = sboConnection.oCompany;
                SAPbobsCOM.UserTable table = table = oCompany.UserTables.Item("SOL_MASTER_OUTLET");

                table.UserFields.Fields.Item("U_SOL_CARD_CODE").Value = parameter.kodePelanggan;
                table.UserFields.Fields.Item("U_SOL_CARD_NAME").Value = parameter.namaPelanggan;
                table.UserFields.Fields.Item("U_SOL_STREET").Value = parameter.alamatPelanggan;
                table.UserFields.Fields.Item("U_SOL_PAY_TERMS").Value = parameter.kodeTermOfPayment;
                table.UserFields.Fields.Item("U_SOL_CUST_GROUP").Value = parameter.kodeTypeOutlet;
                table.UserFields.Fields.Item("U_SOL_GROUP_OUTLET").Value = parameter.kodeGroupOutlet;
                table.UserFields.Fields.Item("U_SOL_GROUP_HARGA").Value = parameter.kodeGroupHarga;
                table.UserFields.Fields.Item("U_SOL_DEFAULT_PEMBAYARAN").Value = parameter.defaultTypePembayaran;
                table.UserFields.Fields.Item("U_SOL_FLAG").Value = parameter.flagOutletRegister;
                table.UserFields.Fields.Item("U_SOL_KODE_CABANG").Value = parameter.kodeDistributor;

                if (table.Add() != 0)
                {
                    sboConnection.oCompany.Disconnect();

                    string objectLog = "OUTLET - ADD";
                    string time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                    string status = "ERROR";
                    string errorMsg = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                    connectionSqlServer.Open();

                    string sqlQuery = $@"INSERT INTO SOL_DI_API_LOG (SOL_OBJECT, SOL_TIME, SOL_STATUS, SOL_ERROR_MESSAGE) VALUES ('{objectLog}', '{time}', '{status}',  '{errorMsg}')";
                    SqlCommand cmd = new SqlCommand(sqlQuery, connectionSqlServer);
                    cmd.ExecuteNonQuery();

                    connectionSqlServer.Close();

                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                    {
                        responseCode = "500",
                        responseMessage = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""),

                    });
                }
                else
                {
                    sboConnection.oCompany.Disconnect();

                    string objectLog = "OUTLET - ADD";
                    string time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                    string status = "SUCCESS";
                    string errorMsg = "";

                    connectionSqlServer.Open();

                    string sqlQuery = $@"INSERT INTO SOL_DI_API_LOG (SOL_OBJECT, SOL_TIME, SOL_STATUS, SOL_ERROR_MESSAGE) VALUES ('{objectLog}', '{time}', '{status}',  '{errorMsg}')";
                    SqlCommand cmd = new SqlCommand(sqlQuery, connectionSqlServer);
                    cmd.ExecuteNonQuery();

                    connectionSqlServer.Close();

                    return StatusCode(StatusCodes.Status200OK, new StatusResponse
                    {
                        responseCode = "200",
                        responseMessage = "Outlet " + parameter.kodePelanggan + " created.",

                    });
                }
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

        [HttpPut("/sapapi/sfaintegration/outlet/update")]
        [Authorize]
        public IActionResult UpdateOutlet([FromBody] Outlet parameter)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            try
            {
                string code = "";

                var connection = new HanaConnection(_connectionStringHana);
                var connectionSqlServer = new SqlConnection(_connectionStringSqlServer);

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_CODE_UDT_OUTLET('" + parameter.kodePelanggan + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "UDT Code not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    code = reader["Code"].ToString();
                                }
                            }
                        }
                    }
                    connection.Close();
                }

                if (parameter.flagOutletRegister == "C")
                {
                    parameter.flagOutletRegister = "Y";
                }

                if (parameter.defaultTypePembayaran == "K")
                {
                    parameter.defaultTypePembayaran = "Kredit";
                }
                else if (parameter.defaultTypePembayaran == "T")
                {
                    parameter.defaultTypePembayaran = "Tunai";
                }

                SAPbobsCOM.Company oCompany = sboConnection.oCompany;
                SAPbobsCOM.UserTable table = table = oCompany.UserTables.Item("SOL_MASTER_OUTLET");


                if (table.GetByKey(code))
                {
                    if(parameter.namaPelanggan != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_CARD_NAME").Value = parameter.namaPelanggan;
                    }
                    if (parameter.alamatPelanggan != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_STREET").Value = parameter.alamatPelanggan;
                    }
                    if (parameter.kodeTermOfPayment != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_PAY_TERMS").Value = parameter.kodeTermOfPayment;
                    }
                    if (parameter.kodeTypeOutlet != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_CUST_GROUP").Value = parameter.kodeTypeOutlet;
                    }
                    if (parameter.kodeGroupOutlet != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_GROUP_OUTLET").Value = parameter.kodeGroupOutlet;
                    }
                    if (parameter.kodeGroupHarga != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_GROUP_HARGA").Value = parameter.kodeGroupHarga;
                    }
                    if (parameter.defaultTypePembayaran != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_DEFAULT_PEMBAYARAN").Value = parameter.defaultTypePembayaran;
                    }
                    if (parameter.flagOutletRegister != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_FLAG").Value = parameter.flagOutletRegister;
                    }
                    if (parameter.kodeDistributor != "")
                    {
                        table.UserFields.Fields.Item("U_SOL_KODE_CABANG").Value = parameter.kodeDistributor;
                    }

                    if (table.Update() != 0)
                    {
                        sboConnection.oCompany.Disconnect();

                        sboConnection.oCompany.Disconnect();

                        string objectLog = "OUTLET - UPDATE";
                        string time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                        string status = "ERROR";
                        string errorMsg = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        connectionSqlServer.Open();

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = "Update Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""),

                        });
                    }
                    else
                    {
                        sboConnection.oCompany.Disconnect();

                        string objectLog = "OUTLET - UPDATE";
                        string time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                        string status = "SUCCESS";
                        string errorMsg = "";

                        connectionSqlServer.Open();

                        string sqlQuery = $@"INSERT INTO SOL_DI_API_LOG (SOL_OBJECT, SOL_TIME, SOL_STATUS, SOL_ERROR_MESSAGE) VALUES ('{objectLog}', '{time}', '{status}',  '{errorMsg}')";
                        SqlCommand cmd = new SqlCommand(sqlQuery, connectionSqlServer);
                        cmd.ExecuteNonQuery();

                        connectionSqlServer.Close();

                        return StatusCode(StatusCodes.Status200OK, new StatusResponse
                        {
                            responseCode = "200",
                            responseMessage = "Outlet " + parameter.kodePelanggan + " updated.",

                        });
                    }
                }
                else
                {
                    sboConnection.oCompany.Disconnect();

                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                    {
                        responseCode = "500",
                        responseMessage = "UDT Code not valid.",

                    });
                }
                
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
