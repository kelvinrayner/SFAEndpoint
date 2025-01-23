using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection.Metadata;
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
        public IActionResult PostOutlet([FromBody] List<OutletParameter> requests)
        {
            Data data = new Data();
            FeedbackNOO feedback = new FeedbackNOO();

            SBOConnection sboConnection = new SBOConnection();
            sboConnection.connectSBO();

            var connectionHana = new HanaConnection(_connectionStringHana);

            try
            {
                List<FeedbackNOO> listFeedbackNOO = new List<FeedbackNOO>();

                foreach (var request in requests)
                {
                    string outletCodeSAP = "";

                    using (connectionHana)
                    {
                        connectionHana.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_CUST_CODE_SAP('" + request.kodePelanggan + "')";

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

                    if (request.flagOutletRegister == "C")
                    {
                        request.flagOutletRegister = "Y";
                    }

                    if (request.defaultTypePembayaran == "K")
                    {
                        request.defaultTypePembayaran = "Kredit";
                    }
                    else if (request.defaultTypePembayaran == "T")
                    {
                        request.defaultTypePembayaran = "Tunai";
                    }

                    if (string.IsNullOrEmpty(request.kodePelangganSAP))
                    {
                        request.kodePelangganSAP = "";
                    }

                    SAPbobsCOM.Company oCompany = sboConnection.oCompany;
                    SAPbobsCOM.UserTable table = table = oCompany.UserTables.Item("SOL_MASTER_OUTLET");

                    table.UserFields.Fields.Item("U_SOL_CARD_CODE").Value = request.kodePelanggan;
                    table.UserFields.Fields.Item("U_SOL_CARD_CODE_SAP").Value = request.kodePelangganSAP;
                    table.UserFields.Fields.Item("U_SOL_CARD_NAME").Value = request.namaPelanggan;
                    table.UserFields.Fields.Item("U_SOL_STREET").Value = request.alamatPelanggan;
                    table.UserFields.Fields.Item("U_SOL_PAY_TERMS").Value = request.kodeTermOfPayment;
                    table.UserFields.Fields.Item("U_SOL_CUST_GROUP").Value = request.kodeTypeOutlet;
                    table.UserFields.Fields.Item("U_SOL_GROUP_OUTLET").Value = request.kodeGroupOutlet;
                    table.UserFields.Fields.Item("U_SOL_GROUP_HARGA").Value = request.kodeGroupHarga;
                    table.UserFields.Fields.Item("U_SOL_DEFAULT_PEMBAYARAN").Value = request.defaultTypePembayaran;
                    table.UserFields.Fields.Item("U_SOL_FLAG").Value = request.flagOutletRegister;
                    table.UserFields.Fields.Item("U_SOL_KODE_CABANG").Value = request.kodeDistributor;
                    table.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;

                    if (table.Add() != 0)
                    {
                        sboConnection.oCompany.Disconnect();

                        string objectLog = "OUTLET - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Add Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg);

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = errorResponse.Substring(0, 255)

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
                            custNoSFA = request.kodePelanggan,
                            date = DateTime.Now,
                            refInterfaceId = request.sfaRefrenceNumber,
                            custNoSAP = outletCodeSAP
                        };

                        listFeedbackNOO.Add(feedback);
                    }
                }

                data = new Data
                {
                    data = listFeedbackNOO
                };

                return StatusCode(StatusCodes.Status201Created, new StatusResponseData
                {
                    data = data
                });
            }
            catch (Exception ex)
            {
                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Substring(0, 255),

                });
            }
        }

        [HttpPut("/sapapi/sfaintegration/outlet/update")]
        [Authorize]
        public IActionResult UpdateOutlet([FromBody] List<OutletParameter> requests)
        {
            Data data = new Data();
            FeedbackNOO feedback = new FeedbackNOO();

            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            try
            {
                List<FeedbackNOO> listFeedbackNOO = new List<FeedbackNOO>();

                foreach (var request in requests) 
                {
                    string code = "";
                    string outletCodeSAP = "";

                    var connection = new HanaConnection(_connectionStringHana);
                    var connectionSqlServer = new SqlConnection(_connectionStringSqlServer);

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_CODE_UDT_OUTLET('" + request.kodePelanggan + "')";

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

                        string queryStringOutletCode = "CALL SOL_SP_ADDON_SFA_INT_CUST_CODE_SAP('" + request.kodePelanggan + "')";

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

                    if (request.flagOutletRegister == "C")
                    {
                        request.flagOutletRegister = "Y";
                    }

                    if (request.defaultTypePembayaran == "K")
                    {
                        request.defaultTypePembayaran = "Kredit";
                    }
                    else if (request.defaultTypePembayaran == "T")
                    {
                        request.defaultTypePembayaran = "Tunai";
                    }

                    SAPbobsCOM.Company oCompany = sboConnection.oCompany;
                    SAPbobsCOM.UserTable table = table = oCompany.UserTables.Item("SOL_MASTER_OUTLET");


                    if (table.GetByKey(code))
                    {
                        if (request.namaPelanggan != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_CARD_NAME").Value = request.namaPelanggan;
                        }
                        if (request.alamatPelanggan != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_STREET").Value = request.alamatPelanggan;
                        }
                        if (request.kodeTermOfPayment != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_PAY_TERMS").Value = request.kodeTermOfPayment;
                        }
                        if (request.kodeTypeOutlet != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_CUST_GROUP").Value = request.kodeTypeOutlet;
                        }
                        if (request.kodeGroupOutlet != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_GROUP_OUTLET").Value = request.kodeGroupOutlet;
                        }
                        if (request.kodeGroupHarga != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_GROUP_HARGA").Value = request.kodeGroupHarga;
                        }
                        if (request.defaultTypePembayaran != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_DEFAULT_PEMBAYARAN").Value = request.defaultTypePembayaran;
                        }
                        if (request.flagOutletRegister != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_FLAG").Value = request.flagOutletRegister;
                        }
                        if (request.kodeDistributor != "")
                        {
                            table.UserFields.Fields.Item("U_SOL_KODE_CABANG").Value = request.kodeDistributor;
                        }

                        if (table.Update() != 0)
                        {
                            sboConnection.oCompany.Disconnect();

                            string objectLog = "OUTLET - UPDATE";
                            string status = "ERROR";
                            string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                            string errorMsg = "Update Outlet Failed, " + oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                            log.insertLog(objectLog, status, errorMsg);

                            return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                            {
                                responseCode = "500",
                                responseMessage = errorResponse.Substring(0, 255),

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
                                custNoSFA = request.kodePelanggan,
                                date = DateTime.Now,
                                refInterfaceId = request.sfaRefrenceNumber,
                                custNoSAP = outletCodeSAP
                            };

                            listFeedbackNOO.Add(feedback);
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

                data = new Data
                {
                    data = listFeedbackNOO
                };

                return StatusCode(StatusCodes.Status201Created, new StatusResponseData
                {
                    data = data

                });
            }
            catch (Exception ex)
            {
                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Substring(0, 255),

                });
            }
        }
    }
}
