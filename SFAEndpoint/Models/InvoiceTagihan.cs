namespace SFAEndpoint.Models
{
    public class InvoiceTagihan
    {
        public int docEntrySAP { get; set; }
        public string kodeCustomer { get; set; }
        public string noInvoice { get; set; }
        public DateTime tanggalInvoice { get; set; }
        public DateTime tanggalInvoiceJatuhTempo { get; set; }
        public decimal nilaiInvoice { get; set; }
        public decimal nilaiInvoiceTerbayar { get; set; }
        public string kodeSalesman { get; set; }
        public string kodeCabang { get; set; }
        public string invoiceType { get; set; }
        public string tanggalTagih { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
