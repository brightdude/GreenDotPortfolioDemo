using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Dapper;

using AdRevenueAggregation;
using AdRevenueAggregation.Managers;
using AdRevenueAggregation.MODEL;


namespace AdRevenueAggregation.Managers
{
    public class SpringServeManager
    {
        private static SpringserveJWT jwt { get; set; }
        public static Authentication Auth { get; set; }
        private static SpringserveJWT getToken(ILogger log)
        {
            log.LogInformation("getToken");
            var client = new RestClient("https://console.springserve.com/api/v0/auth");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(Auth);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            jwt = JsonConvert.DeserializeObject<SpringserveJWT>(client.Execute(request).Content);
            return jwt;

        }
        private static string SpringserveAPI(ILogger log, Method httpMethod, string APIMethod, string body = "")
        {
            log.LogInformation($"SpringserveAPI:APIMethod={APIMethod}:Body={body}");
            string baseURL = @"https://console.springserve.com/api/v0/";
            var client = new RestClient($"{baseURL}{APIMethod}");
            client.Timeout = -1;
            var request = new RestRequest(httpMethod);
            if (!string.IsNullOrEmpty(body))
            {
                request.AddParameter("undefined", body, ParameterType.RequestBody);
                request.AddHeader("Content-Type", "application/json; charset=utf-8");
            }
            request.AddHeader("Authorization", $"{getToken(log).token}");
            return client.Execute(request).Content;
        }
        public static string SaveReportDays(ILogger log) {
            log.LogInformation($"GetReport");
            try {
                DBRepo.Log = log;
                var _start_date = (DBRepo.SpringserveReportDays_LastDay());
                var start_date = $"{_start_date.Year}-{_start_date.Month}-{_start_date.Day + 1}";
                SpringserveReportRequest request = new SpringserveReportRequest()
                { 
                    start_date = start_date,
                    end_date = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}",
                    dimensions = new List<string>() { "supply_type", "campaign_id","content_id", "content_title", "content_custom1_param","content_custom2_param", "content_custom3_param", "demand_tag_id" },
                    interval = "day"
                };
                /* see   https://springserve.atlassian.net/wiki/spaces/SSD/pages/1588035603/Reporting+API#ReportingAPI-Availableparameters */
                var reportraw = SpringserveAPI(log, Method.POST, "report", JsonConvert.SerializeObject(request,Formatting.Indented));
                var report = JsonConvert.DeserializeObject<List<reportItem>>(reportraw, new JsonSerializerSettings() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Include });

                var cleanerdata = report.Where(w =>  w.content_custom2_param != null && w.content_custom3_param != null && !string.IsNullOrWhiteSpace(w.content_custom2_param) && !string.IsNullOrWhiteSpace(w.content_custom3_param)).ToList();

                cleanerdata.ForEach(reportItem => {
                    DBRepo.springserveReportDays_set(reportItem);
                });
            }
            catch (Exception e) {
                log.LogError("err", e);
            }
            return "OK";
        }

    }
}
