using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

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
        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/outlet/master")]
        [Authorize]
        public IActionResult GetAllOutlet()
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
                                        kodeDistributor = reader["kodeDistributor"].ToString()
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

        [HttpPost("/sapapi/sfaintegration/outlet")]
        [Authorize]
        public IActionResult GetSpesificOutlet(OutletSpesificParameter parameter)
        {
            OutletSpesific outlet = new OutletSpesific();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_OUTLET_SPESIFIC('" + parameter.kodePelangganSFA + "')";

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
                                    outlet = new OutletSpesific
                                    {
                                        kodePelangganSFA = reader["kodePelangganSFA"].ToString(),
                                        kodePelanggan = reader["kodePelangganSAP"].ToString(),
                                        namaPelanggan = reader["namaPelanggan"].ToString()
                                    };
                                }

                                data = new Data
                                {
                                    data = outlet
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
        public IActionResult PostOutlet([FromBody] OutletParameter parameter)
        {
            Data data = new Data();
            FeedbackNOO feedback = new FeedbackNOO();

            SBOConnection sboConnection = new SBOConnection();
            sboConnection.connectSBO();

            var connectionHana = new HanaConnection(_connectionStringHana);

            try
            {
                string outletCodeSAP = "";

                using (connectionHana)
                {
                    connectionHana.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_CUST_CODE_SAP('" + parameter.kodePelanggan + "')";

                    using (var command = new HanaCommand(queryString, connectionHana))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    outletCodeSAP = reader["CardCode"].ToString();
                                }
                            }
                        }
                    }
                    connectionHana.Close();
                }

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

                if (string.IsNullOrEmpty(parameter.kodePelangganSAP))
                {
                    parameter.kodePelangganSAP = "";
                }

                SAPbobsCOM.Company oCompany = sboConnection.oCompany;
                SAPbobsCOM.UserTable table = table = oCompany.UserTables.Item("SOL_MASTER_OUTLET");

                table.UserFields.Fields.Item("U_SOL_CARD_CODE").Value = parameter.kodePelanggan;
                table.UserFields.Fields.Item("U_SOL_CARD_CODE_SAP").Value = parameter.kodePelangganSAP;
                table.UserFields.Fields.Item("U_SOL_CARD_NAME").Value = parameter.namaPelanggan;
                table.UserFields.Fields.Item("U_SOL_STREET").Value = parameter.alamatPelanggan;
                table.UserFields.Fields.Item("U_SOL_PAY_TERMS").Value = parameter.kodeTermOfPayment;
                table.UserFields.Fields.Item("U_SOL_CUST_GROUP").Value = parameter.kodeTypeOutlet;
                table.UserFields.Fields.Item("U_SOL_GROUP_OUTLET").Value = parameter.kodeGroupOutlet;
                table.UserFields.Fields.Item("U_SOL_GROUP_HARGA").Value = parameter.kodeGroupHarga;
                table.UserFields.Fields.Item("U_SOL_DEFAULT_PEMBAYARAN").Value = parameter.defaultTypePembayaran;
                table.UserFields.Fields.Item("U_SOL_FLAG").Value = parameter.flagOutletRegister;
                table.UserFields.Fields.Item("U_SOL_KODE_CABANG").Value = parameter.kodeDistributor;
                table.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = parameter.sfaRefrenceNumber;

                if (table.Add() != 0)
                {
                    sboConnection.oCompany.Disconnect();

                    string objectLog = "OUTLET - ADD";
                    string status = "ERROR";
                    string errorMsg = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                    log.insertLog(objectLog, status, errorMsg);

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
                    string status = "SUCCESS";
                    string errorMsg = "";

                    log.insertLog(objectLog, status, errorMsg);

                    feedback = new FeedbackNOO
                    {
                        custNoSFA = parameter.kodePelanggan,
                        date = DateTime.Now,
                        refInterfaceId = parameter.sfaRefrenceNumber,
                        custNoSAP = outletCodeSAP
                    };

                    data = new Data
                    {
                        data = feedback
                    };

                    return Ok(data);
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
        public IActionResult UpdateOutlet([FromBody] OutletParameter parameter)
        {
            Data data = new Data();
            FeedbackNOO feedback = new FeedbackNOO();

            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            try
            {
                string code = "";
                string outletCodeSAP = "";

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

                    string queryStringOutletCode = "CALL SOL_SP_ADDON_SFA_INT_CUST_CODE_SAP('" + parameter.kodePelanggan + "')";

                    using (var commandOutletCode = new HanaCommand(queryString, connection))
                    {
                        using (var readerOutletCode = commandOutletCode.ExecuteReader())
                        {
                            if (readerOutletCode.HasRows)
                            {
                                while (readerOutletCode.Read())
                                {
                                    outletCodeSAP = readerOutletCode["CardCode"].ToString();
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

                        string objectLog = "OUTLET - UPDATE";
                        string status = "ERROR";
                        string errorMsg = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg);

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
                        string status = "SUCCESS";
                        string errorMsg = "";

                        log.insertLog(objectLog, status, errorMsg);

                        feedback = new FeedbackNOO
                        {
                            custNoSFA = parameter.kodePelanggan,
                            date = DateTime.Now,
                            refInterfaceId = parameter.sfaRefrenceNumber,
                            custNoSAP = outletCodeSAP
                        };

                        data = new Data
                        {
                            data = feedback
                        };

                        return Ok(data);
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
