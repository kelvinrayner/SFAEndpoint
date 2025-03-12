namespace SFAEndpoint.Models
{
    public class InventoryTransferRequest
    {
        public int docEntrySAP { get; set; }
        public string docNumSAP { get; set; }
        public int salesCode { get; set; }
        public string docDate { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<InventoryTransferRequestDetail> detail { get; set; }
    }
}
