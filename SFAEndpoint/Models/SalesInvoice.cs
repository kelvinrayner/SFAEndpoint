namespace SFAEndpoint.Models
{
    public class SalesInvoice
    {
        public string kodeSalesman { get; set; }
        public string kodeCustomer { get; set; }
        public string orderNoERP { get; set; }
        public DateTime orderDateERP { get; set; }
        public string noInvoiceERP { get; set; }
        public string tanggalInvoice { get; set; }
        public List<SalesInvoiceDetail> detail { get; set; }
        public string kodeCabang { get; set; }
        public string invoiceType { get; set; }
        public decimal invoiceAmount { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
