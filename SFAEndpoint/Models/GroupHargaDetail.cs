namespace SFAEndpoint.Models
{
    public class GroupHargaDetail
    {
        public string kodeGroupHarga { get; set; } = String.Empty;
        public string kodeProdukPrincipal { get; set; } = String.Empty;
        public decimal hargaJualKecil { get; set; } = 0;
        public decimal hargaJualTengah { get; set; } = 0;
        public decimal hargaJualBesar { get; set; } = 0;
    }
}
