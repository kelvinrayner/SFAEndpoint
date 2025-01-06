namespace SFAEndpoint.Models.Parameter
{
    public class ReturnParameter
    {
        public string cardCode { get; set; }
        public DateTime tanggal { get; set; }
        public int salesCode { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<ReturnDetailParameter> detail { get; set; }
    }
}
