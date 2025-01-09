namespace SFAEndpoint.Models.Parameter
{
    public class StockRequestDetailParameter
    {
        public string kodeProdukPrincipal { get; set; }
        public double quantity { get; set; }
        public string fromWarehouse { get; set; }
        public string fromBinCode { get; set; }
        public string toWarehouse { get; set; }
        public string toBinCode { get; set; }
    }
}
