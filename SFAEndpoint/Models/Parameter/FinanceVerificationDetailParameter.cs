﻿namespace SFAEndpoint.Models.Parameter
{
    public class FinanceVerificationDetailParameter
    {
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public double quantity { get; set; }
        public double price { get; set; }
        public string warehouseCode { get; set; }
    }
}
