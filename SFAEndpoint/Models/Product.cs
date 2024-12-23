namespace SFAEndpoint.Models
{
    public class Product
    {
        public string kodeProductLine{ get; set; } = String.Empty;
        public string kodeProduct { get; set; } = String.Empty;
        public string kodeProductPrincipal { get; set; } = String.Empty;
        public string deskripsiProduct { get; set; } = String.Empty;
        public string uomBesar { get; set; } = String.Empty;
        public string uomTengah { get; set; } = String.Empty;
        public string uomKecil { get; set; } = String.Empty;
        public int konversiTengah { get; set; } = 0;
        public int konversiBesar { get; set; } = 0;
        public decimal pajak { get; set; } = 0;
    }
}
