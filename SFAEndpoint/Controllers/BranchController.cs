using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sap.Data.Hana;
using SFAEndpoint.Models;

namespace SFAEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly string _connectionStringHana;

        public BranchController(IConfiguration configuration)
        {
            _connectionStringHana = configuration.GetConnectionString("SapHanaConnection");
        }

        Data data = new Data();

        [HttpPost("/sapapi/sfaintegration/branch/master")]
        [Authorize]
        public IActionResult GetBranch()
        {
            Branch branch = new Branch();

            var connection = new HanaConnection(_connectionStringHana);

            try
            {
                List<Branch> listBranch = new List<Branch>();

                using (connection)
                {
                    connection.Open();

                    string queryString = "CALL SOL_SP_ADDON_SFA_INT_GET_MASTER_BRANCH()";

                    using (var command = new HanaCommand(queryString, connection))
                    {

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return StatusCode(StatusCodes.Status404NotFound, new StatusResponse
                                {
                                    responseCode = "404",
                                    responseMessage = "Branch not found.",

                                });
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    branch = new Branch
                                    {
                                        kodeBranch = Convert.ToInt32(reader["PrcCode"]),
                                        deskripsiBranch = reader["PrcName"].ToString()
                                    };

                                    listBranch.Add(branch);
                                }

                                data = new Data
                                {
                                    data = listBranch
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
