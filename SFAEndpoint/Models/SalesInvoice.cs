namespace SFAEndpoint.Models
{
    public class SalesInvoice
    {
        public string kodeSalesman { get; set; } = String.Empty;
        public string kodeCustomer { get; set; } = String.Empty;
        public string orderNoERP { get; set; } = String.Empty;
        public string orderDateERP { get; set; } = String.Empty;
        public string noInvoiceERP { get; set; } = String.Empty;
        public string tanggalInvoice { get; set; } = String.Empty;
        public List<SalesInvoiceDetail> salesInvoiceDetails { get; set; } = new List<SalesInvoiceDetail>();
        public string kodeDistributor { get; set; } = String.Empty;
        public string invoiceType { get; set; } = String.Empty;
        public double invoiceAmount { get; set; } = 0;
    }
}
