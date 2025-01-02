using System.Data.SqlClient;

namespace SFAEndpoint.Services
{
    public class InsertDILogService
    {
        private readonly string connectionStringSqlServer = "server=SVR-PPNT;database=soldb;uid=sa;password=sqlPPNT*168;";

        public void insertLog(string objectLog, string status, string errorMsg)
        {
            var connectionSqlServer = new SqlConnection(connectionStringSqlServer);

            string time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            connectionSqlServer.Open();

            string sqlQuery = $@"INSERT INTO SOL_DI_API_LOG (SOL_OBJECT, SOL_TIME, SOL_STATUS, SOL_ERROR_MESSAGE) VALUES ('{objectLog}', '{time}', '{status}',  '{errorMsg}')";
            SqlCommand cmd = new SqlCommand(sqlQuery, connectionSqlServer);
            cmd.ExecuteNonQuery();

            connectionSqlServer.Close();
        }
    }
}
