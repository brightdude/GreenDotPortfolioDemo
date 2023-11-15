
using System;
using System.Collections.Generic;
using System.Text;

namespace AdRevenueAggregation.MODEL
{
    public class Authentication
    {
        public string email { get; set; }
        public string password { get; set; }

    }
    public class SpringserveJWT
    {
        public string token { get; set; }
    }
    public partial class SpringserveReportRequest
    {
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string interval { get; set; }
        public List<string> dimensions { get; set; }
    }
    public class reportItem
    {
        public string date { get; set; }
        public string supply_type { get; set; }
        public string demand_tag_name { get; set; }
        public string demand_tag_id { get; set; }
        public string campaign_id { get; set; }
        public string campaign_name { get; set; }
        public string content_title { get; set; }
        public string content_id { get; set; }
        public string content_custom1_param { get; set; }
        public string content_custom2_param { get; set; }
        public string content_custom3_param { get; set; }
        public string router_usable_requests { get; set; }
        public string router_fallback_requests { get; set; }
        public string demand_requests { get; set; }
        public string bids { get; set; }
        public string router_missed_opportunities { get; set; }
        public string routed_missed_requests { get; set; }
        public string wins { get; set; }
        public string impressions { get; set; }
        public string breakout_impressions { get; set; }
        public string starts { get; set; }
        public string router_usable_request_rate { get; set; }
        public string routed_missed_request_rate { get; set; }
        public string router_opp_rate { get; set; }
        public string bid_rate { get; set; }
        public string use_rate { get; set; }
        public string win_rate { get; set; }
        public string router_request_fill_rate { get; set; }
        public string fill_rate { get; set; }
        public string efficiency_rate { get; set; }
        public string win_fill_rate { get; set; }
        public string revenue { get; set; }
        public string cost { get; set; }
        public string profit { get; set; }
        public string margin { get; set; }
        public string rpm { get; set; }
        public string rpmr { get; set; }
        public string cpm { get; set; }
        public string ppm { get; set; }
        public string score { get; set; }
        public string clicks { get; set; }
        public string click_through_rate { get; set; }
        public string first_quartile { get; set; }
        public string second_quartile { get; set; }
        public string third_quartile { get; set; }
        public string fourth_quartile { get; set; }
        public string fourth_quartile_rate { get; set; }
    }


    public class reportList {
        List<reportItem> reportItems { get; set; }
    }
}
