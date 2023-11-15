using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster.TenantSettings
{
    internal class Channel
    {
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring       
        private readonly IAuthorisationService _authService;

        public Channel(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        /// <summary>
        /// Returns a list of all channels from the tenant settings.
        /// </summary>
        [OpenApiOperation(operationId: "channels-get", Summary = "/channels - GET", Description = "Retrieves a list of all channels")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChannelDescription[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of channels")]
        [RequiredPermission("settings.ReadAll")]
        [Function("TenantSettings_Channel_RetrieveAll")]
        public async Task<HttpResponseData> ChannelRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/channels")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(ChannelRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var settings = await _tenantSettingsService.GetTenantSettings();
                var result = (settings == null || settings.Channels == null) ? Array.Empty<ChannelDescription>() : settings.Channels;
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JArray.FromObject(result);
                await retrievedResponse.WriteStringAsync(obj.ToString());
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
