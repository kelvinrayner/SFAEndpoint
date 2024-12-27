using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using Microsoft.Extensions.Caching.Memory;
using SFAEndpoint.Models.Parameter;
using Microsoft.AspNetCore.Authorization;

namespace SFAEndpoint.Controllers
{
    public class PaymentTermsController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public PaymentTermsController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        //[HttpPost("/sapapi/sfaintegration/paymentterms/master/all")]
        //public IActionResult GetAllPaymentTerms()
        //{
        //    PaymentTerms paymentTerms = new PaymentTerms();

        //    var connection = new HanaConnection(_connectionStringHana);

        //    try
        //    {
        //        List<PaymentTerms> listPaymentTerms = new List<PaymentTerms>();

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_TOP(0)";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {

        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        return StatusCode(StatusCodes.Status404NotFound, new ErrorResponse
        //                        {
        //                            responseCode = "404",
        //                            responseMessage = "Payment Terms not found.",

        //                        });
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            paymentTerms = new PaymentTerms
        //                            {
        //                                kodeTop = reader["kodeTop"].ToString(),
        //                                deskripsiTop = reader["deskripsiTop"].ToString(),
        //                                jumlahHari = reader["jumlahHari"].ToString(),
        //                            };

        //                            listPaymentTerms.Add(paymentTerms);
        //                        }

        //                        data = new Data
        //                        {
        //                            data = listPaymentTerms
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

        [HttpPost("/sapapi/sfaintegration/paymentterms/master")]
        [Authorize]
        public IActionResult GetSpesificPaymentTerms()
        {
            PaymentTerms paymentTerms = new PaymentTerms();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<PaymentTerms> listPaymentTerms = new List<PaymentTerms>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_MASTER_TOP(0)";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Payment Terms not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    paymentTerms = new PaymentTerms
                                    {
                                        kodeTop = reader["kodeTop"].ToString(),
                                        deskripsiTop = reader["deskripsiTop"].ToString(),
                                        jumlahHari = reader["jumlahHari"].ToString(),
                                    };

                                    listPaymentTerms.Add(paymentTerms);
                                }

                                data = new Data
                                {
                                    data = listPaymentTerms
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
