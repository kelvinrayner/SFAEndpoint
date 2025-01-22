namespace SFAEndpoint.Models.Parameter
{
    public class ReturnExistingParameter
    {
        public string cardCode { get; set; }
        public DateOnly tanggal { get; set; }
        public DateOnly? tanggalARCM { get; set; }
        public int salesCode { get; set; }
        public string docnumDeliveryOrder { get; set; }
        public List<ReturnDetailExistingParameter> detail { get; set; }
    }
}
