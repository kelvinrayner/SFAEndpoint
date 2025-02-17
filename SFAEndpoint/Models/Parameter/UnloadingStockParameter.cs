namespace SFAEndpoint.Models.Parameter
{
    public class UnloadingStockParameter
    {
        public int salesCode { get; set; }
        public string fromWarehouse { get; set; }
        public string? toWarehouse { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<UnloadingStockDetailParameter> detail { get; set; }
    }
}
