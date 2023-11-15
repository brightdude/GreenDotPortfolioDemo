using System;
using System.Collections.Generic;
using System.Text;

namespace AdRevenueAggregation
{
    public class VStarRecord
    {/// <summary>
     /// select 'public string '+ replace(name,' ','') +' { get; set; }' from sys.columns c where object_name(object_id) = 'VStarFeed'  order by c.column_id
     /// </summary>
        public string Day { get; set; }
        public string BidderName { get; set; }
        public string Buyer { get; set; }
        public string Advertiser { get; set; }
        public string Creative { get; set; }
        public string CreativeID { get; set; }
        public string VenueID { get; set; }
        public string VenueName { get; set; }
        public string Impressions { get; set; }
        public string Spots { get; set; }
        public string Revenue { get; set; }
        public string eCPM { get; set; }
        public string DataeCPM { get; set; }
        public string DataCost { get; set; }
        public string Profit { get; set; }
    }
}
