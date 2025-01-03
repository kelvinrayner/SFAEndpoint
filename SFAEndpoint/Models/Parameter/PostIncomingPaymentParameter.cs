namespace SFAEndpoint.Models.Parameter
{
    public class PostIncomingPaymentParameter
    {
        public string kodePelanggan { get; set; }
        public DateTime tanggal { get; set; }
        public string bankAccount { get; set; }
        public string totalAmount { get; set; }
        public int docEntryARInvSAP { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
