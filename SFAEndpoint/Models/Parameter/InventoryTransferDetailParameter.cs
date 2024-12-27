namespace SFAEndpoint.Models.Parameter
{
    public class InventoryTransferDetailParameter
    {
        public int lineNumSAP { get; set; }
        public string itemCode { get; set; }
        public double quantity { get; set; }
    }
}
