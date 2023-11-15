using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Dapper;
using ExcelDataReader.Log;
using Microsoft.Extensions.Logging;

using AdRevenueAggregation;
using AdRevenueAggregation.Managers;
using AdRevenueAggregation.MODEL;



namespace AdRevenueAggregation
{
    public class TargetRManager
    {
        public static async Task<String> pullFeed(ILogger log)
        {
            var client = new RestClient("https://rmtv-api-1.herokuapp.com/dashboard/v1/screens/");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            string gpsParams= AppHelper.GetSecretFromAzureKeyVault("TargetRManager-gpsParams", "https://your-keyvault-name.vault.azure.net");
            request.AddParameter("application/json", gpsParams, ParameterType.RequestBody);
            var content = "";
            IRestResponse response = client.Execute(request);
            content = response.Content;
            return content;
        }

        public static async Task<List<TargetRScreen>> GetScreens(ILogger log)
        {
            string feedOutput = await pullFeed(log);


            List<TargetRScreen> screenList = JsonConvert.DeserializeObject<IEnumerable<TargetRScreen>>(feedOutput, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            }).ToList();

            screenList = screenList.Where(o =>
                //(o.data.rtvServiceStatus == "InService" || o.data.rtvServiceStatus == "Pending Install")
                //&&
                o.data.rtvServiceStatus != "Not InService"
                &&
                o.data.rtvAirportCode != "RTV"
                &&
                o.data.rtvAirportCode != "undefined"
                &&
                o.data.rtvPuckType != "Demo"
                &&
                (o.data.rtvPuckType == "Airport Puck" || o.data.rtvPuckType == "Gate")
                &&
                o.data.rtvConcessionaire != "RMT"
                &&
                o.data.rtvConcessionaire != "JHI"
                &&
                o.data.rtvConcessionaire != "OHM"
                &&
                o.data.rtvConcessionaire != "PGC"

            ).ToList();

            screenList = screenList.Where(o => (o.data.rtvVenueId != null && o.data.rtvTerminal != null) || o.data.rtvPuckType == "Gate").ToList();
            screenList.ForEach(item => { DBRepo.TargetRScreenData_set(item); });

            return screenList;
        }
    }
}
