﻿using System.Reflection.Metadata;
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
    public class StockRequestController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public StockRequestController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        [HttpPost("/sapapi/sfaintegration/stockrequest/new")]
        [Authorize]
        public IActionResult PostStockRequest([FromBody] List<StockRequestParameter> requests)
        {
            SBOConnection sboConnection = new SBOConnection();

            sboConnection.connectSBO();

            var connection = new HanaConnection(_connectionStringHana);
            InsertDILogService log = new InsertDILogService();
            List<InsertLog> listLog = new List<InsertLog>();
            string sfaRefNum = "";

            //Declare all SAPbobsCOM untuk DI API UDO
            SAPbobsCOM.GeneralService oGeneralService = null;
            SAPbobsCOM.GeneralData oGeneralData = null;
            SAPbobsCOM.GeneralDataCollection oSons = null;
            SAPbobsCOM.GeneralData oSon = null;

            SAPbobsCOM.CompanyService oSTR = null;
            oSTR = sboConnection.oCompany.GetCompanyService();

            //Get a handle to the Stock Request UDO
            oGeneralService = oSTR.GetGeneralService("OSTR");

            //Specify data for main UDO (Header)
            oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);

            try
            {
                sboConnection.oCompany.StartTransaction();

                foreach (var request in requests)
                {
                    sfaRefNum = request.skaRefrenceNumber;

                    DateTime tanggal = request.requestDate.ToDateTime(TimeOnly.MinValue);

                    oGeneralData.SetProperty("U_SOL_SALES_CODE", request.salesCode);
                    oGeneralData.SetProperty("U_SOL_SALES_NAME", request.salesName);
                    oGeneralData.SetProperty("U_SOL_REF_SKA_NUM", sfaRefNum);
                    oGeneralData.SetProperty("U_SOL_REQ_DATE", tanggal);
                    oGeneralData.SetProperty("U_SOL_STATUS", "OPEN");

                    foreach (var detail in request.detail)
                    {
                        string itemCode = "";
                        string itemName = "";
                        string whsCode = "";

                        using (connection)
                        {
                            connection.Open();

                            string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_ITEM_CODE('" + detail.kodeProdukPrincipal + "')";

                            using (var command = new HanaCommand(queryString, connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            itemCode = reader["ItemCode"].ToString();
                                            itemName = reader["ItemName"].ToString();
                                        }
                                    }
                                }
                            }

                            string queryStringWhsCode = "CALL SOL_SP_ADDON_SFA_INT_GET_WHS_BIN_CODE('" + detail.toBinCode + "')";

                            using (var commandWhsCode = new HanaCommand(queryStringWhsCode, connection))
                            {
                                using (var readerWhsCode = commandWhsCode.ExecuteReader())
                                {
                                    if (readerWhsCode.HasRows)
                                    {
                                        while (readerWhsCode.Read())
                                        {
                                            whsCode = readerWhsCode["WhsCode"].ToString();
                                        }
                                    }
                                }
                            }

                            connection.Close();
                        }

                        //Specify data for child UDO
                        oSons = oGeneralData.Child("SOL_D_STOCK_REQ");
                        oSon = oSons.Add();
                        oSon.SetProperty("U_SOL_ITEM_PRINCIPAL", detail.kodeProdukPrincipal);
                        oSon.SetProperty("U_SOL_ITEM_CODE", itemCode);
                        oSon.SetProperty("U_SOL_ITEM_NAME", itemName);
                        oSon.SetProperty("U_SOL_QUANTITY", detail.quantity);
                        oSon.SetProperty("U_SOL_FROM_WHS", detail.fromWarehouse);
                        oSon.SetProperty("U_SOL_FROM_BIN", detail.fromBinCode);
                        oSon.SetProperty("U_SOL_TO_WHS", whsCode);
                        oSon.SetProperty("U_SOL_TO_BIN", detail.toBinCode);
                    }

                    //Add records
                    oGeneralService.Add(oGeneralData);

                    var logData = new InsertLog
                    {
                        objectLog = "STOCK REQUEST - ADD",
                        status = "SUCCESS",
                        errorMessage = "",
                        sfaRefNumber = sfaRefNum
                    };

                    listLog.Add(logData);
                }
                sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                foreach (var dataLog in listLog)
                {
                    log.insertLog(dataLog.objectLog, dataLog.status, dataLog.errorMessage, dataLog.sfaRefNumber);
                }

                if (oGeneralData != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralData);
                    oGeneralData = null;
                }

                if (oGeneralService != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralService);
                    oGeneralService = null;
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
                    responseMessage = "Stock Request added to SAP."
                });
            }
            catch (Exception ex)
            {
                if (sboConnection.oCompany.InTransaction)
                {
                    sboConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }

                string objectLog = "STOCK REQUEST  - ADD";
                string status = "ERROR";
                string errorResponse = sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");
                string errorMsg = "Create Stock Request Failed, " + sboConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", "");

                if (oGeneralData != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralData);
                    oGeneralData = null;
                }

                if (oGeneralService != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGeneralService);
                    oGeneralService = null;
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

                log.insertLog(objectLog, status, errorMsg, sfaRefNum);

                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
                {
                    responseCode = "500",
                    responseMessage = errorResponse.Length > 255 ? errorResponse.Substring(0, 255) : errorResponse,
                });
            }
        }
    }
}
