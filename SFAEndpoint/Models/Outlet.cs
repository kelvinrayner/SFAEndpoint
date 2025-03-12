namespace SFAEndpoint.Models
{
    public class Outlet
    {
        public string kodePelanggan { get; set; }
        public string kodePelangganSFA { get; set; }
        public string namaPelanggan { get; set; }
        public string alamatPelanggan { get; set; }
        public string kodeTermOfPayment { get; set; }
        public string kodeTypeOutlet { get; set; }
        public string kodeGroupOutlet { get; set; }
        public string kodeGroupHarga { get; set; }
        public string defaultTypePembayaran { get; set; }
        public string flagOutletRegister { get; set; }
        public string kodeDistributor { get; set; }
        public decimal totalPlafondCredit { get; set; }
		public decimal totalOutstandingInvoice { get; set; }
	    public decimal sisaKreditLimit { get; set; }
    }
}
