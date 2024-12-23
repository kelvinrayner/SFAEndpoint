namespace SFAEndpoint.Models
{
    public class InvoiceTagihan
    {
        public string kodeCustomer { get; set; } = String.Empty;
        public string noInvoice { get; set; } = String.Empty;
        public DateTime tanggalInvoice { get; set; } = DateTime.Now;
        public DateTime tanggalInvoiceJatuhTempo { get; set; } = DateTime.Now;
        public decimal nilaiInvoice { get; set; } = 0;
        public decimal nilaiInvoiceTerbayar { get; set; } = 0;
        public string kodeSalesman { get; set; } = String.Empty;
        public string kodeDistributorCabang { get; set; } = String.Empty;
        public string invoiceType { get; set; } = String.Empty;
        public DateTime tanggalTagih { get; set; } = DateTime.Now;
    }
}
