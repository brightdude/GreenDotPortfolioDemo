using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Polly;

namespace AdRevenueAggregation
{
    public static class fnTargetRScreens
    {
        [FunctionName("fnTargetRScreens")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Guard.Against.Null(req, nameof(req));
            Guard.Against.Null(log, nameof(log));

            List<TargetRScreen> screenList = await TargetRManager.GetScreens(log);
            Guard.Against.Null(screenList, nameof(screenList));
            return new OkObjectResult(screenList);
        }
    }
}
