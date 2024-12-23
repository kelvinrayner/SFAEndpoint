namespace SFAEndpoint.Models
{
    public class Return
    {
        public string kodeSalesman { get; set; } = string.Empty;
        public string kodeCustomer { get; set; } = string.Empty;
        public string orderNoERP { get; set; } = string.Empty;
        public string orderDateERP { get; set; } = string.Empty;
        public string noInvoiceERP { get; set; } = string.Empty;
        public string tanggalInvoice { get; set; } = string.Empty;
        public List<ReturnDetail> returnDetails { get; set; } = new List<ReturnDetail>();
        public string kodeDistributor { get; set; } = string.Empty;
        public string invoiceType { get; set; } = string.Empty;
        public double invoiceAmount { get; set; } = 0;
    }
}
