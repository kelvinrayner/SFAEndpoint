namespace SFAEndpoint.Models
{
    public class SalesInvoice
    {
        public string kodeSalesman { get; set; }
        public string kodeCustomer { get; set; }
        public string orderNoERP { get; set; }
        public string orderDateERP { get; set; }
        public string noInvoiceERP { get; set; }
        public string tanggalInvoice { get; set; }
        public List<SalesInvoiceDetail> salesInvoiceDetails { get; set; }
        public string kodeDistributor { get; set; }
        public string invoiceType { get; set; }
        public double invoiceAmount { get; set; }
    }
}
