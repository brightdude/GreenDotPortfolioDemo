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
    internal class StreamType
    {
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring       
        private readonly IAuthorisationService _authService;

        public StreamType(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        /// <summary>
        /// Returns a list of all stream types from the tenant settings.
        /// </summary>
        [OpenApiOperation(operationId: "streamTypes-get", Summary = "/streamTypes - GET", Description = "Retrieves a list of all stream types")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of stream types")]
        [RequiredPermission("settings.ReadAll")]
        [Function("TenantSettings_StreamType_RetrieveAll")]
        public async Task<HttpResponseData> StreamTypeRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/streamTypes")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(StreamTypeRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var settings = await _tenantSettingsService.GetTenantSettings();
                var result = (settings == null || settings.StreamTypeValues == null) ? Array.Empty<LookupItem>() : settings.StreamTypeValues;
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
