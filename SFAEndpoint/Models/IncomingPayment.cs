namespace SFAEndpoint.Models
{
    public class IncomingPayment
    {
        public string kodeCustomer { get; set; }
        public string noInvoiceERP { get; set; }
        public DateTime tanggalInvoice { get; set; }
        public decimal invoiceAmount { get; set; }
        public string docEntryARInvSAP { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
