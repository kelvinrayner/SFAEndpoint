﻿namespace SFAEndpoint.Models
{
    public class StockRequestDetail
    {
        public int lineId { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public double quantity { get; set; }
        public string fromWarehouse { get; set; }
        public string fromBinCode { get; set; }
        public string toWarehouse { get; set; }
        public string toBinCode { get; set; }
    }
}
