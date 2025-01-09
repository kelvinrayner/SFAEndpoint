namespace SFAEndpoint.Models
{
    public class SalesInvoiceDetail
    {
        public int lineNumSAP { get; set; }
        public string kodeProduk { get; set; }
        public string kodeProdukPrincipal { get; set; }
        public decimal qtyInPcs { get; set; }
        public decimal priceValue { get; set; }
        public decimal discountValue { get; set; }
    }
}
