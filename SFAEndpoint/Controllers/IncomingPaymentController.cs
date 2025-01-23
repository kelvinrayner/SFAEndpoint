using System.Data.Common;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SAPbobsCOM;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingPaymentController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public IncomingPaymentController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        InsertDILogService log = new InsertDILogService();

        [HttpPost("/sapapi/sfaintegration/incomingpayment")]
        [Authorize]
        public IActionResult GetIncoming([FromBody] IncomingPaymentParameter parameter)
        {
            Data data = new Data();
            IncomingPayment incomingPayment = new IncomingPayment();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_INCOMING_PAY('" + parameter.sfaRefrenceNumber + "')";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Incoming Payment not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    incomingPayment = new IncomingPayment
                                    {
                                        kodeCustomer = reader["kodeCustomer"].ToString(),
                                        noInvoiceERP = reader["noInvoiceERP"].ToString(),
                                        tanggalInvoice = Convert.ToDateTime(reader["tanggalInvoice"]),
                                        invoiceAmount = Convert.ToDecimal(reader["invoiceAmount"]),
                                        sfaRefrenceNumber = reader["sfaRefrenceNumber"].ToString()
                                    };
                                }

                                data = new Data
                                {
                                    data = incomingPayment
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

        [HttpPost("/sapapi/sfaintegration/incomingpayment/new")]
        [Authorize]
        public IActionResult PostIncoming([FromBody] List<PostIncomingPaymentParameter> requests)
        {
            var connection = new HanaConnection(_connectionStringHana);

            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            try
            {
                foreach (var request in requests)
                {
                    DateTime tanggal = request.tanggal.ToDateTime(TimeOnly.MinValue);

                    double docTotalAR = 0;

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_DOCTOTAL_ARINV(" + request.docEntryARInvSAP + ")";

                        using (var command = new HanaCommand(queryString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        docTotalAR = Convert.ToDouble(reader["DocTotal"]);
                                    }
                                }
                            }
                        }

                        connection.Close();
                    }

                    SAPbobsCOM.Payments oIncomingPayments;
                    oIncomingPayments = sboConnection.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oIncomingPayments);

                    oIncomingPayments.DocType = SAPbobsCOM.BoRcptTypes.rCustomer;
                    oIncomingPayments.CardCode = request.kodePelanggan;
                    oIncomingPayments.DocDate = tanggal;
                    oIncomingPayments.DocCurrency = "IDR";
                    //oIncomingPayments.LocalCurrency = SAPbobsCOM.BoYesNoEnum.tYES;
                    oIncomingPayments.BankAccount = request.bankAccount;
                    //oIncomingPayments.BankCode = "BRI";
                    oIncomingPayments.TransferAccount = request.bankAccount;
                    oIncomingPayments.TransferDate = tanggal;
                    oIncomingPayments.TransferSum = Convert.ToDouble(request.totalAmount);
                    oIncomingPayments.UserFields.Fields.Item("U_SOL_SFA_REF_NUM").Value = request.sfaRefrenceNumber;

                    // Add the AR Invoice
                    oIncomingPayments.Invoices.DocLine = 0;
                    oIncomingPayments.Invoices.InvoiceType = SAPbobsCOM.BoRcptInvTypes.it_Invoice;
                    oIncomingPayments.Invoices.DocEntry = Convert.ToInt32(request.docEntryARInvSAP);

                    int retval = oIncomingPayments.Add();

                    if (retval != 0)
                    {
                        sboConnection.oCompany.Disconnect();

                        string objectLog = "INCOMING PAYMENT - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Create Incoming Payment Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg);

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = errorResponse.Substring(0, 255),
                        });
                    }
                    else
                    {
                        string objectLog = "INCOMING PAYMENT - ADD";
                        string status = "SUCCESS";
                        string errorMsg = "";

                        log.insertLog(objectLog, status, errorMsg);
                    }
                }
                sboConnection.oCompany.Disconnect();

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Incoming Payment added to SAP."
                });
            }
            catch (HanaException hx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ("HANA Error: " + hx.Message).Substring(0, 255),

                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Substring(0, 255),

                });
            }
        }
    }
}
