﻿using System.Data.Common;
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
                                        tanggalInvoice = reader["tanggalInvoice"].ToString(),
                                        invoiceAmount = Convert.ToDecimal(reader["invoiceAmount"]),
                                        docEntryARInvSAP = reader["docEntryARInvSAP"].ToString(),
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

            List<InsertLog> listLog = new List<InsertLog>();
            string sfaRefNum = "";

            SAPbobsCOM.Payments oIncomingPayments = null;
            oIncomingPayments = sboConnection.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oIncomingPayments);

            try
            {
                sboConnection.oCompany.StartTransaction();

                foreach (var request in requests)
                {
                    DateTime tanggal = request.tanggal.ToDateTime(TimeOnly.MinValue);
                    sfaRefNum = request.sfaRefrenceNumber;
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
                        string objectLog = "INCOMING PAYMENT - ADD";
                        string status = "ERROR";
                        string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                        string errorMsg = "Create Incoming Payment Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                        log.insertLog(objectLog, status, errorMsg, request.sfaRefrenceNumber);

                        if (oIncomingPayments != null)
                        {
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIncomingPayments);
                            oIncomingPayments = null;
                        }

                        if (sboConnection.oCompany != null)
                        {
                            if (sboConnection.oCompany.Connected)
                            {
                                sboConnection.oCompany.Disconnect();
                            }
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                            sboConnection.oCompany = null;
                        }

                        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                        {
                            responseCode = "500",
                            responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                        });
                    }
                    else
                    {
                        var logData = new InsertLog
                        {
                            objectLog = "INCOMING PAYMENT - ADD",
                            status = "SUCCESS",
                            errorMessage = "",
                            sfaRefNumber = sfaRefNum
                        };

                        listLog.Add(logData);
                    }
                }
                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                foreach (var dataLog in listLog)
                {
                    log.insertLog(dataLog.objectLog, dataLog.status, dataLog.errorMessage, dataLog.sfaRefNumber);
                }

                if (oIncomingPayments != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIncomingPayments);
                    oIncomingPayments = null;
                }

                if (sboConnection.oCompany != null)
                {
                    if (sboConnection.oCompany.Connected)
                    {
                        sboConnection.oCompany.Disconnect();
                    }
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                    sboConnection.oCompany = null;
                }

                return StatusCode(StatusCodes.Status201Created, new StatusResponse
                {
                    responseCode = "201",
                    responseMessage = "Incoming Payment added to SAP."
                });
            }
            catch (HanaException hx)
            {
                string objectLog = "INCOMING PAYMENT - ADD";
                string status = "ERROR";
                string errorMsg = "Create Incoming Payment Failed, " + hx.Message;

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ("HANA Error: " + hx.Message).Substring(0, 255),

                });
            }
            catch (Exception ex)
            {
                if (sboConnection.oCompany.InTransaction)
                {
                    sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }

                string objectLog = "INCOMING PAYMENT - ADD";
                string status = "ERROR";
                string errorMsg = "Create Incoming Payment Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                if (oIncomingPayments != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oIncomingPayments);
                    oIncomingPayments = null;
                }

                if (sboConnection.oCompany != null)
                {
                    if (sboConnection.oCompany.Connected)
                    {
                        sboConnection.oCompany.Disconnect();
                    }
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sboConnection.oCompany);
                    sboConnection.oCompany = null;
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = ex.Message.Length > 255 ? ex.Message.Substring(0, 255) : ex.Message,

                });
            }
        }
    }
}
