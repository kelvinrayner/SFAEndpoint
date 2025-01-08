namespace SFAEndpoint.Models.Parameter
{
    public class FinanceVerificationParameter
    {
        public string customerCode { get; set; }
        public string customerName { get; set; }
        public int salesCode { get; set; }
        public string salesName { get; set; }
        public string skaRefrenceNumber { get; set; }
        public DateOnly requestDate { get; set; }
        public string wilayah { get; set; }
        public string accountTransfer { get; set; }
        public List<FinanceVerificationDetailParameter> detail { get; set; }
    }
}
