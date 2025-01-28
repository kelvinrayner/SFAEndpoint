namespace SFAEndpoint.Models
{
    public class oCreditMemoLines
    {
        public string kodeProdukPrincipal { get; set; }
        public string itemCode { get; set; }
        public double quantity { get; set; }
        public string warehouseCode { get; set; }
        public string branch { get; set; }
        public string productGroup { get; set; }
        public string brand { get; set; }
        public string salesPerson { get; set; }
        public string customerGroup { get; set; }
        public double lineTotal { get; set; }
        public int docEntryARInv { get; set; }
        public int baseLine { get; set; }
        public int lineNum { get; set; }
    }
}
