namespace SFAEndpoint.Models.Parameter
{
    public class SalesOrderDetailParameter
    {
        public string kodeProdukPrincipal { get; set; }
        public double quantity { get; set; }
        public double unitPrice { get; set; }
        public string? warehouseCode { get; set; }
        public string? kodeCabang { get; set; }
    }
}
