namespace SFAEndpoint.Models
{
    public class SalesInvoiceDetail
    {
        public string kodeProduct { get; set; }
        public int qtyInPcs { get; set; }
        public double priceValue { get; set; }
        public double discountValue { get; set;}
    }
}
