namespace SFAEndpoint.Models.Parameter
{
    public class InventoryTransferParameter
    {
        //public DateTime date { get; set; }
        public int docEntrySAP { get; set; }
        public int salesCode { get; set; }
        //public string fromWarehouse { get; set; }
        //public string toWarehouse { get; set; }
        public string sfaRefrenceNumber { get; set; }
        public List<InventoryTransferDetailParameter> detail { get; set; }
    }
}
