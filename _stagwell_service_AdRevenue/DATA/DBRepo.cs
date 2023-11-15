using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Dapper;
using Ardalis.GuardClauses;
using AdRevenueAggregation.MODEL;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


namespace AdRevenueAggregation
{

    public class AppHelper
    {
        public static string GetSecretFromAzureKeyVault(string secretName, string keyVaultUri)
        {
            var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            try
            {
                KeyVaultSecret secret = secretClient.GetSecret(secretName);
                return secret.Value;
            }
            catch (Exception ex)
            {
                return null; 
            }
        }
        private readonly static string sqlConnection = GetSecretFromAzureKeyVault("sqlConnectionSECRET", "https://your-keyvault-name.vault.azure.net");
        public static Func<DbConnection> ConnectionFactory = () => new SqlConnection(sqlConnection);
        
    }

    public class DBRepo
    {
        public static ILogger Log { get; set; }
        public string Filename { get; set; }
        public static async Task PlaceExchangeSetAsync(PlaceExchangeRecord model)
        {
            Guard.Against.Null(model, nameof(model));

            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var p = new DynamicParameters();
                        p.Add("OrgName", model.OrgName);
                        p.Add("Advertiser", model.Advertiser);
                        p.Add("AdunitName", model.AdunitName);
                        p.Add("PlayTsDate", model.PlayTsDate);
                        p.Add("PayerName", model.PayerName);
                        p.Add("NetworkName", model.NetworkName);
                        p.Add("BuyerName", model.BuyerName);
                        p.Add("CreativeURL", model.CreativeURL);
                        p.Add("CreativePreview", model.CreativePreview);
                        p.Add("CreativeID", model.CreativeID);
                        p.Add("NumDistinctImpressions", model.NumDistinctImpressions);
                        p.Add("NumDistinctPlays", model.NumDistinctPlays);
                        p.Add("ClearingPriceAmount", model.ClearingPriceAmount);
                        p.Add("MediaCostAmount", model.MediaCostAmount);
                        p.Add("PubClearingPriceEcpm", model.PubClearingPriceEcpm);
                        p.Add("PubMediaCostEcpm", model.PubMediaCostEcpm);

                        await connection.ExecuteAsync("[dbo].[PlaceExchangeFeed_Set]", p, commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }

        public static async Task VStarSet_Async(VStarRecord model)
        {
            Guard.Against.Null(model, nameof(model));

            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var p = new DynamicParameters();
                        p.Add("Day", model.Day);
                        p.Add("BidderName", model.BidderName);
                        p.Add("Buyer", model.Buyer);
                        p.Add("Advertiser", model.Advertiser);
                        p.Add("Creative", model.Creative);
                        p.Add("CreativeID", model.CreativeID);
                        p.Add("VenueID", model.VenueID);
                        p.Add("VenueName", model.VenueName);
                        p.Add("Impressions", model.Impressions);
                        p.Add("Spots", model.Spots);
                        p.Add("Revenue", model.Revenue);
                        p.Add("eCPM", model.eCPM);
                        p.Add("DataeCPM", model.DataeCPM);
                        p.Add("DataCost", model.DataCost);
                        p.Add("Profit", model.Profit);

                        await connection.ExecuteAsync("[dbo].[VStar_Set]", p, commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }
        public static async Task PlaceExchangeCommitCatchup_Async()
        {
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        await connection.ExecuteAsync("[dbo].[PlaceExchangeCommitCatchup]", commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }

        public static async Task VStarCommitCatchup_Async()
        {
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        await connection.ExecuteAsync("[dbo].[Vstar_CommitCatchup]", commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }
        public static List<string> PlaceExchange_getFilename()
        {
            IEnumerable<string> dto = Enumerable.Empty<string>();
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var result = connection.Query<string>("[dbo].[PlaceExchange_getFilename]");
                        dto = (result.Count() > 0) ? result : dto;
                        return dto.ToList();
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
                return null;
            }
        }
        public static async Task PlaceExchange_SetFilename(string filename)
        {
            Guard.Against.Null(filename, nameof(filename));
            IEnumerable<string> dto = Enumerable.Empty<string>();
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var p = new DynamicParameters();
                        p.Add("filename", filename);
                        await connection.ExecuteAsync("[dbo].[PlaceExchange_setFilename]", p, commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }
        public static async Task VStar_SetFilename(string filename)
        {
            Guard.Against.Null(filename, nameof(filename));
            IEnumerable<string> dto = Enumerable.Empty<string>();
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var p = new DynamicParameters();
                        p.Add("filename", filename);
                        await connection.ExecuteAsync("[dbo].[VStar_setFilename]", p, commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }
        }
        public static List<string> VStar_getFilename()
        {

            IEnumerable<string> dto = Enumerable.Empty<string>();
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var result = connection.Query<string>("[dbo].[VStar_getFilename]");
                        dto = (result.Count() > 0) ? result : dto;
                        return dto.ToList();
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
                return null;
            }
        }

        public static DateTime SpringserveReportDays_LastDay()
        {
            IEnumerable<DateTime> dto = Enumerable.Empty<DateTime>();
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var result = connection.Query<DateTime>("[dbo].[SpringserveReportDays_LastDay]");
                        dto = (result.Count() > 0) ? result : dto;
                        return dto.FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
                throw e;
            }
        }

        public static void TargetRScreenData_set(TargetRScreen model)
        {
            int TargetRScreenID = 0;
            try
            {
                using (var connection = AppHelper.ConnectionFactory())
                {
                    connection.Open();
                    {
                        var p = new DynamicParameters();
                        p.Add("Type", model.type);
                        p.Add("Id", model.id);
                        p.Add("SequenceId", model.sequenceId);
                        /// if exists TargetRScreen_set return 0
                        TargetRScreenID = connection.Query<int>("[dbo].[TargetRScreen_set]", p, commandType: CommandType.StoredProcedure).First();

                    }
                }
                if (TargetRScreenID != 0)
                {
                    using (var connection = AppHelper.ConnectionFactory())
                    {
                        connection.Open(); /// select 'p.Add("' + name + '", model.data.' + name + CASE WHEN c.user_type_id = 231 then ' ?? String.Empty'  else ' ?? 0 '  end +');' from sys.columns c  where object_name(object_id) = 'TargetRScreenData' order by c.column_id
                        {
                            var p = new DynamicParameters();
                            p.Add("TargetRScreenID", TargetRScreenID);
                            p.Add("gpsLongitude", model.data.gpsLongitude ?? String.Empty);
                            p.Add("rtvTerminal", model.data.rtvTerminal ?? String.Empty);
                            p.Add("rtvTeamViewer", model.data.rtvTeamViewer ?? String.Empty);
                            p.Add("lastPlayerCommsMillis", model.data.lastPlayerCommsMillis ?? String.Empty);
                            p.Add("requiredDataAvailable", model.data.requiredDataAvailable ?? String.Empty);
                            p.Add("desiredPlayer", model.data.desiredPlayer ?? String.Empty);
                            p.Add("wifiSsid", model.data.wifiSsid ?? String.Empty);
                            p.Add("locality", model.data.locality ?? String.Empty);
                            p.Add("timeZone", model.data.timeZone ?? String.Empty);
                            p.Add("lastLoaderCommsMillis", model.data.lastLoaderCommsMillis ?? String.Empty);
                            p.Add("rtvVenue_Location", model.data.rtvVenue_Location ?? String.Empty);
                            p.Add("rtvCountry", model.data.rtvCountry ?? String.Empty);
                            p.Add("rtvAirportCode", model.data.rtvAirportCode ?? String.Empty);
                            p.Add("rtvVenueId", model.data.rtvVenueId ?? String.Empty);
                            p.Add("rtvDemoAirportCode", model.data.rtvDemoAirportCode ?? String.Empty);
                            p.Add("startCount", model.data.startCount ?? String.Empty);
                            p.Add("memFree", model.data.memFree ?? String.Empty);
                            p.Add("rtvGate_Area", model.data.rtvGate_Area ?? String.Empty);
                            p.Add("rtvScreenType", model.data.rtvScreenType ?? String.Empty);
                            p.Add("hardwareDevice", model.data.hardwareDevice ?? String.Empty);
                            p.Add("rtvWapStatus", model.data.rtvWapStatus ?? String.Empty);
                            p.Add("blobDataQueueSize", model.data.blobDataQueueSize ?? String.Empty);
                            p.Add("rtvLBar", model.data.rtvLBar ?? String.Empty);
                            p.Add("requiredDataTotal", model.data.requiredDataTotal ?? String.Empty);
                            p.Add("rtvAffiliate", model.data.rtvAffiliate ?? String.Empty);
                            p.Add("lastLoaderUdpCommsMillis", model.data.lastLoaderUdpCommsMillis ?? String.Empty);
                            p.Add("modifiedMillis", model.data.modifiedMillis ?? String.Empty);
                            p.Add("hardwareBuild", model.data.hardwareBuild ?? String.Empty);
                            p.Add("rtvDedicated", model.data.rtvDedicated ?? String.Empty);
                            p.Add("sequenceId", model.data.sequenceId ?? String.Empty);
                            p.Add("startMillis", model.data.startMillis ?? String.Empty);
                            p.Add("hardwareModel", model.data.hardwareModel ?? String.Empty);
                            p.Add("rtvPuckType", model.data.rtvPuckType ?? String.Empty);
                            p.Add("region", model.data.region ?? String.Empty);
                            p.Add("rtvWapMac", model.data.rtvWapMac ?? String.Empty);
                            p.Add("rtvState", model.data.rtvState ?? String.Empty);
                            p.Add("rtvConcessionaire", model.data.rtvConcessionaire ?? String.Empty);
                            p.Add("rtvDemoConcessionaire", model.data.rtvDemoConcessionaire ?? String.Empty);
                            p.Add("customStatus", model.data.customStatus ?? String.Empty);
                            p.Add("street", model.data.street ?? String.Empty);
                            p.Add("freeSpace_0", model.data.freeSpace_0 ?? String.Empty);
                            p.Add("activePlayer", model.data.activePlayer ?? String.Empty);
                            p.Add("freeSpace_1", model.data.freeSpace_1 ?? String.Empty);
                            p.Add("gpsLatitude", model.data.gpsLatitude ?? String.Empty);
                            p.Add("memTotal", model.data.memTotal ?? String.Empty);
                            p.Add("rtvCity", model.data.rtvCity ?? String.Empty);
                            p.Add("rtvWapName", model.data.rtvWapName ?? String.Empty);
                            p.Add("label", model.data.label ?? String.Empty);
                            p.Add("rtvScreen", model.data.rtvScreen ?? String.Empty);
                            p.Add("rtvWap", model.data.rtvWap ?? String.Empty);
                            p.Add("blobDataDownloadFailures", model.data.blobDataDownloadFailures ?? String.Empty);
                            p.Add("rtvAirportName", model.data.rtvAirportName ?? String.Empty);
                            p.Add("rtvWapClients", model.data.rtvWapClients ?? String.Empty);
                            p.Add("rtvServiceStatus", model.data.rtvServiceStatus ?? String.Empty);
                            p.Add("online", model.data.online ?? String.Empty);
                            p.Add("offline24", model.data.offline24 ?? String.Empty);
                            p.Add("offline48", model.data.offline48 ?? String.Empty);
                            p.Add("commDiff", model.data.commDiff ?? String.Empty);
                            p.Add("udpCommDiff", model.data.udpCommDiff ?? String.Empty);
                            p.Add("commStatus", model.data.commStatus ?? String.Empty);
                            p.Add("hdmiState", model.data.hdmiState ?? String.Empty);
                            p.Add("localAddress", model.data.localAddress ?? String.Empty);
                            p.Add("subnetId", model.data.subnetId ?? String.Empty);
                            p.Add("rtvComplianceCount", model.data.rtvComplianceCount ?? String.Empty);
                            p.Add("loaderVersion", model.data.loaderVersion ?? String.Empty);
                            p.Add("backupServerUsed", model.data.backupServerUsed ?? String.Empty);
                            p.Add("notes", model.data.notes ?? String.Empty);
                            p.Add("healthStatus", model.data.healthStatus ?? String.Empty);
                            p.Add("firewallBlock", model.data.firewallBlock ?? String.Empty);
                            p.Add("recentStartCount", model.data.recentStartCount ?? String.Empty);
                            p.Add("applicationCrashed", model.data.applicationCrashed ?? String.Empty);
                            p.Add("blobDataDownloadedRate", model.data.blobDataDownloadedRate ?? String.Empty);
                            p.Add("loaderPlayerCommDiff", model.data.loaderPlayerCommDiff ?? String.Empty);
                            p.Add("displayDownloadedRate", model.data.displayDownloadedRate ?? String.Empty);
                            p.Add("blobDataActiveDownloads", model.data.blobDataActiveDownloads ?? String.Empty);
                            p.Add("slowDownloadSpeed", model.data.slowDownloadSpeed ?? String.Empty);
                            p.Add("localPeerCount", model.data.localPeerCount ?? String.Empty);
                            p.Add("insufficientPeers", model.data.insufficientPeers ?? String.Empty);
                            p.Add("udpStatus", model.data.udpStatus ?? String.Empty);
                            p.Add("restartStatus", model.data.restartStatus ?? String.Empty);
                            p.Add("coportServiceStatus", model.data.coportServiceStatus ?? String.Empty);
                            p.Add("rtvTeamViewerOn", model.data.rtvTeamViewerOn ?? String.Empty);
                            p.Add("rtvHDMI", model.data.rtvHDMI ?? String.Empty);
                            p.Add("rtvGate", model.data.rtvGate ?? String.Empty);
                            p.Add("numTVs", model.data.numTVs ?? String.Empty);

                            connection.Execute("[dbo].[TargetRScreenData_set]", p, commandType: CommandType.StoredProcedure);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(1150, e, e.Message);
            }

        }


        public static void springserveReportDays_set(reportItem model)
        {
            using (var connection = AppHelper.ConnectionFactory())
            {
                connection.Open(); /// select 'p.Add("' + name + '", model.' + name+');' from sys.columns c  where object_name(object_id) = 'SpringserveReportDays' order by c.column_id
                {
                    var p = new DynamicParameters();

                    p.Add("date", model.date);
                    p.Add("supply_type", model.supply_type);
                    p.Add("demand_tag_id", model.demand_tag_id);
                    p.Add("demand_tag_name", model.demand_tag_name);
                    p.Add("campaign_id", model.campaign_id);
                    p.Add("campaign_name", model.campaign_name);
                    p.Add("content_title", model.content_title);
                    p.Add("content_id", model.content_id);
                    p.Add("content_custom1_param", model.content_custom1_param);
                    p.Add("content_custom2_param", model.content_custom2_param);
                    p.Add("content_custom3_param", model.content_custom3_param);
                    p.Add("router_usable_requests", model.router_usable_requests);
                    p.Add("router_fallback_requests", model.router_fallback_requests);
                    p.Add("demand_requests", model.demand_requests);
                    p.Add("bids", model.bids);
                    p.Add("router_missed_opportunities", model.router_missed_opportunities);
                    p.Add("routed_missed_requests", model.routed_missed_requests);
                    p.Add("wins", model.wins);
                    p.Add("impressions", model.impressions);
                    p.Add("breakout_impressions", model.breakout_impressions);
                    p.Add("starts", model.starts);
                    p.Add("router_usable_request_rate", model.router_usable_request_rate);
                    p.Add("routed_missed_request_rate", model.routed_missed_request_rate);
                    p.Add("router_opp_rate", model.router_opp_rate);
                    p.Add("bid_rate", model.bid_rate);
                    p.Add("use_rate", model.use_rate);
                    p.Add("win_rate", model.win_rate);
                    p.Add("router_request_fill_rate", model.router_request_fill_rate);
                    p.Add("fill_rate", model.fill_rate);
                    p.Add("efficiency_rate", model.efficiency_rate);
                    p.Add("win_fill_rate", model.win_fill_rate);
                    p.Add("revenue", model.revenue);
                    p.Add("cost", model.cost);
                    p.Add("profit", model.profit);
                    p.Add("margin", model.margin);
                    p.Add("rpm", model.rpm);
                    p.Add("rpmr", model.rpmr);
                    p.Add("cpm", model.cpm);
                    p.Add("ppm", model.ppm);
                    p.Add("score", model.score);
                    p.Add("clicks", model.clicks);
                    p.Add("click_through_rate", model.click_through_rate);
                    p.Add("first_quartile", model.first_quartile);
                    p.Add("second_quartile", model.second_quartile);
                    p.Add("third_quartile", model.third_quartile);
                    p.Add("fourth_quartile", model.fourth_quartile);
                    p.Add("fourth_quartile_rate", model.fourth_quartile_rate);

                    try {
                        connection.Execute("[dbo].[SpringserveReportDays_set]", p, commandType: CommandType.StoredProcedure);
                    }
                    catch (Exception ex) 
                    {
                        var err = ex.Message;
                    }
                    
                }
            }
        }
    }
}
