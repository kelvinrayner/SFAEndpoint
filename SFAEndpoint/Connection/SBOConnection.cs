using SAPbobsCOM;

namespace SFAEndpoint.Connection
{
    public class SBOConnection
    {
        public Company oCompany = new Company();

        public void connectSBO()
        {
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "192.168.1.250:40000";
                oCompany.Server = "192.168.1.250:30015";
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = "DEV_PPN";
                oCompany.DbUserName = "SYSTEM";
                oCompany.DbPassword = "@Mest#123";
                oCompany.UserName = "IT01";
                oCompany.Password = "soltius01";
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    throw new Exception("error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }
        }

        public void connectDIAPI(string username, string password)
        {
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "192.168.1.250:40000";
                oCompany.Server = "192.168.1.250:30015";
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = "DEV_PPN";
                oCompany.DbUserName = "SYSTEM";
                oCompany.DbPassword = "@Mest#123";
                oCompany.UserName = username;
                oCompany.Password = password;
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    throw new Exception("error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }
        }
    }
}
