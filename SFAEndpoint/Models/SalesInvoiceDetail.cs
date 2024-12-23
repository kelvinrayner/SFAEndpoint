namespace SFAEndpoint.Models
{
    public class SalesInvoiceDetail
    {
        public string kodeProduct { get; set; }  = String.Empty;
        public int qtyInPcs { get; set; } = 0;
        public double priceValue { get; set; } = 0;
        public double discountValue { get; set;} = 0;
    }
}
