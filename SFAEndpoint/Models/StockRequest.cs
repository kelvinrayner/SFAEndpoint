namespace SFAEndpoint.Models
{
    public class StockRequest
    {
        public int salesCode { get; set; }
        public string salesName { get; set; }
        public string skaRefNum { get; set; }
        public string requestDate { get; set; }
        public string status { get; set; }
        public List<StockRequestDetail> stockRequestDetail { get; set; }
    }
}
