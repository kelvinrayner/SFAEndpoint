namespace SFAEndpoint.Models.Parameter
{
    public class ReturnDetailParameter
    {
        public string itemCode { get; set; }
        public double quantity { get; set; }
        public string warehouseCode { get; set; } = String.Empty;
    }
}
