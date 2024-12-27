namespace SFAEndpoint.Models
{
    public class InventoryTransferRequestDetail
    {
        public int lineNumSAP { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public double qty { get; set; }
    }
}
