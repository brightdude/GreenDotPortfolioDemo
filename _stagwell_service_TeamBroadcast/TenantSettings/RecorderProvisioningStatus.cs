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
    internal class RecorderProvisioningStatus
    {
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring       
        private readonly IAuthorisationService _authService;

        public RecorderProvisioningStatus(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        /// <summary>
        /// Returns a list of all recorder provisioning status values from the tenant settings.
        /// </summary>
        [OpenApiOperation(operationId: "provisioningstatusvalues-get", Summary = "/provisioningstatusvalues - GET", Description = "Retrieves a list of all access levels")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of provisioning status values")]
        [RequiredPermission("settings.ReadAll")]
        [Function("TenantSettings_RecorderProvisioningStatus_RetrieveAll")]
        public async Task<HttpResponseData> RecorderProvisioningStatusRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/provisioningstatusvalues")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(RecorderProvisioningStatusRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var settings = await _tenantSettingsService.GetTenantSettings();
                var result = (settings == null || settings.RecorderProvisioningStatusValues == null) ? Array.Empty<string>() : settings.RecorderProvisioningStatusValues;
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
