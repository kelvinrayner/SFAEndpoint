namespace SFAEndpoint.Models
{
    public class SalesOrder
    {
        public int docEntrySAP { get; set; }
        public string kodeSalesman { get; set; }
        public string kodeCustomer { get; set; }
        public string noSalesOrderERP { get; set; }
        public string tanggalSalesOrder { get; set; }
        public List<SalesOrderDetail> detail { get; set; }
        public string kodeCabang { get; set; }
        public double salesOrderAmount { get; set; }
        public string sfaRefrenceNumber { get; set; }
    }
}
