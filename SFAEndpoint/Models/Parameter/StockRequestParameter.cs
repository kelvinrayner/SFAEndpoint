namespace SFAEndpoint.Models.Parameter
{
    public class StockRequestParameter
    {
        public int salesCode { get; set; }
        public string salesName { get; set; }
        public string skaRefrenceNumber { get; set; }
        public string requestDate { get; set; }
        public List<StockRequestDetailParameter> detail { get; set; }
    }
}