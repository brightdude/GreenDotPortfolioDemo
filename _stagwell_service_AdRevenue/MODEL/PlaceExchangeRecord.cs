using System;
using System.Collections.Generic;
using System.Text;

namespace AdRevenueAggregation
{
    public class PlaceExchangeRecord
    {
        public string OrgName { get; set; }
        public string Advertiser { get; set; }
        public string AdunitName { get; set; }
        public string PlayTsDate { get; set; }
        public string PayerName { get; set; }
        public string NetworkName { get; set; }
        public string BuyerName { get; set; }
        public string CreativeURL { get; set; }
        public string CreativePreview { get; set; }
        public string CreativeID { get; set; }
        public string NumDistinctImpressions { get; set; }
        public string NumDistinctPlays { get; set; }
        public string ClearingPriceAmount { get; set; }
        public string MediaCostAmount { get; set; }
        public string PubClearingPriceEcpm { get; set; }
        public string PubMediaCostEcpm { get; set; }
    }
}
