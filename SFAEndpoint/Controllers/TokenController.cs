using Microsoft.AspNetCore.Mvc;
using SFAEndpoint.Connection;
using SFAEndpoint.Models;
using SFAEndpoint.Models.Parameter;
using SFAEndpoint.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    responseMessage = "Invalid credentials.",

                });
            }
        }
    }
}
