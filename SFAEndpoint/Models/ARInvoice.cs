namespace SFAEndpoint.Models
{
    public class ARInvoice
    {
        public int docEntrySAP { get; set; }
        public string kodeSalesman { get; set; }
        public string kodeCustomer { get; set; }
        public string noInvoiceERP { get; set; }
        public string tanggalInvoice { get; set; }
        public List<ARInvoiceDetail> detail { get; set; }
        public string kodeCabang { get; set; }
        public string invoiceType { get; set; }
        public decimal invoiceAmount { get; set; }
        public string customerRefNumSAP { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
