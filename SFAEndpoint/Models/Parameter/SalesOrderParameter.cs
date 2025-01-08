namespace SFAEndpoint.Models.Parameter
{
    public class SalesOrderParameter
    {
        public string cardCode { get; set; }
        public DateOnly tanggal { get; set; }
        public int salesCode { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<SalesOrderDetailParameter> detail { get; set; }
    }
}
