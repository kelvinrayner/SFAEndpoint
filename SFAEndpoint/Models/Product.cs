namespace SFAEndpoint.Models
{
    public class Product
    {
        public string kodeProductLine{ get; set; }
        public string kodeProduct { get; set; }
        public string kodeProductPrincipal { get; set; }
        public string deskripsiProduct { get; set; }
        public string uomBesar { get; set; }
        public string uomTengah { get; set; }
        public string uomKecil { get; set; }
        public int konversiTengah { get; set; }
        public int konversiBesar { get; set; }
        public decimal pajak { get; set; }
        public int itemGroupCode { get; set; }
        public string itemGroupName { get; set; }
        public string active { get; set; }
    }
}
