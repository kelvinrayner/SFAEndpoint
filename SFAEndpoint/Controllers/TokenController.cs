using Microsoft.AspNetCore.Mvc;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;

namespace SFAEndpoint.Controllers
{
    public class TokenController : ControllerBase
    {
        [HttpPost("/sapapi/sfaintegration/token")]
        public IActionResult GenerateToken([FromBody] TokenParameter tokenParameter)
        {
            try
            {
                Data data = new Data();
                SBOConnection diApiConnection = new SBOConnection();

                diApiConnection.connectDIAPI(tokenParameter.userId, tokenParameter.password);

                if (diApiConnection.oCompany.Connected)
                {
                    var tokenService = new TokenService();
                    var accessToken = tokenService.GenerateToken(tokenParameter.userId);
                    Token token = new Token();

                    token = new Token
                    {
                        token = accessToken,
                        expiresIn = "15 Minutes"
                    };

                    data = new Data
                    {
                        data = token
                    };
                }

                if (diApiConnection.oCompany != null)
                {
                    if (diApiConnection.oCompany.Connected)
                    {
                        diApiConnection.oCompany.Disconnect();
                    }
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(diApiConnection.oCompany);
                    diApiConnection.oCompany = null;
                }

                return Ok(data);
            }
            catch (Exception ex) 
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new StatusResponse
                {
                    responseCode = "401",
                    responseMessage = "Invalid credentials. " + ex.Message,

                });
            }
        }

        //[HttpPost("/sapapi/sfaintegration/token")]
        //public async Task<IActionResult> GenerateTokens([FromBody] TokenParameter tokenParameter)
        //{
        //    try
        //    {
        //        var requestData = new TokenParameter
        //        {
        //            userId = tokenParameter.userId,
        //            password = tokenParameter.password
        //        };

        //        var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
        //        HttpResponseMessage response = await _httpClient.PostAsync("http://192.168.1.92:81/sapapi/token", content);
        //        //response.EnsureSuccessStatusCode();

        //        string responseString = await response.Content.ReadAsStringAsync();
        //        var jsonObj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseString);

        //        return StatusCode((int)response.StatusCode, jsonObj);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse
        //        {
        //            responseCode = "500",
        //            responseMessage =  ex.Message,

        //        }); ;
        //    }
        //}
    }
}
