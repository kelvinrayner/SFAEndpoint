namespace SFAEndpoint.Models
{
    public class ARInvoiceDetail
    {
        public int lineNumSAP { get; set; }
        public string kodeProduk { get; set; }
        public decimal qtyInPcs { get; set; }
        public decimal priceValue { get; set; }
        public decimal discountValue { get; set; }
    }
}
