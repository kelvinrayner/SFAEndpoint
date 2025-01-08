namespace SFAEndpoint.Models.Parameter
{
    public class ReturnParameter
    {
        public string cardCode { get; set; }
        public DateOnly tanggal { get; set; }
        public DateOnly? tanggalARCM { get; set; }
        public int salesCode { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<ReturnDetailParameter> detail { get; set; }
    }
}
