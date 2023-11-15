using System;
using System.IO;
using System.Threading.Tasks;
using Ardalis.GuardClauses;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AdRevenueAggregation;
using AdRevenueAggregation.MODEL;
using AdRevenueAggregation.Managers;



namespace AdRevenueAggregation.Facade
{
    public static class fnSpringserve
    {
        [FunctionName("fnSpringserve")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,ILogger log, ExecutionContext context)
        {
            Guard.Against.Null(context, nameof(context));
            Guard.Against.Null(req, nameof(req));
            Guard.Against.Null(log, nameof(log));

            log.LogInformation("C# HTTP trigger function processed a request.");
            string SpringServePassword = AppHelper.GetSecretFromAzureKeyVault("SpringServePassword", "https://your-keyvault-name.vault.azure.net");
            string SpringServeUser = AppHelper.GetSecretFromAzureKeyVault("SpringServeUser", "https://your-keyvault-name.vault.azure.net");
            Guard.Against.Null(SpringServeUser, nameof(SpringServeUser));
            Guard.Against.Null(SpringServePassword, nameof(SpringServePassword));

            Authentication auth = new Authentication() { email = SpringServeUser, password = SpringServePassword };
            Guard.Against.Null(auth, nameof(auth));

            SpringServeManager.Auth=auth;
            SpringServeManager.SaveReportDays(log);


            return new OkObjectResult("OK");
        }
    }
}
