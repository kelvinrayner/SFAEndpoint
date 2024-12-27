namespace SFAEndpoint.Models
{
    public class InventoryTransferRequest
    {
        public int docEntrySAP { get; set; }
        public string docNumSAP { get; set; }
        public DateTime docDate { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<InventoryTransferRequestDetail> detail { get; set; }
    }
}
