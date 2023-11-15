using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class Facility
    {
        /// <summary>
        /// Returns a list of all Facilities.
        /// </summary>
        [OpenApiOperation(operationId: "facilities-get", Summary = "/facilities - GET", Description = "Returns a list of all of the facilities defined in the system")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Facility[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of facilities")]
        [RequiredPermission("facilities.ReadAll", "facilities.ReadWriteAll")]
        [Function("Facility_RetrieveAll")]
        public async Task<HttpResponseData> FacilityRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "facilities")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(FacilityRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var facilities = await _breezyCosmosService.ListFacilities();
                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(facilities);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Returns a Facility with the specified id.
        /// </summary>
        [OpenApiOperation(operationId: "facilities-facilityid-get", Summary = "​/facilities​/{facilityId} - GET", Description = "Retrieve the details of a facility")]
        [OpenApiParameter(name: "facilityId", Description = "The id of the facility to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Facility), Summary = "Retrieved OK", Description = "Successfully retrieved the facility")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The facility was not found")]
        [RequiredPermission("facilities.ReadAll", "facilities.ReadWriteAll")]
        [Function("Facility_Retrieve")]
        public async Task<HttpResponseData> FacilityRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "facilities/{facilityId}")] HttpRequestData req, FunctionContext executionContext, string facilityId)
        {
            var functionName = nameof(FacilityRetrieve);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            facilityId = (facilityId ?? "").ToLower();

            try
            {
                // Retrieve facility
                var facility = await _breezyCosmosService.GetFacility(facilityId);
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, $"A facility with id '{facilityId}' was not found");
                }

                // Retrieve associated calendars
                var calendars = await _breezyCosmosService.ListCalendars(facilityId);
                facility.Calendars = calendars.Select(c => c.ToCalendarIdentity()).ToArray();

                // Retrieve associated on-demand meetings
                var queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.msTeamId = @msTeamId AND DateTimeAdd(\"hh\", 1, c.endDateTime) > GetCurrentDateTime() AND c.activeFlag = true")
                    .WithParameter("@msTeamId", facility.Team.MsTeamId);
                facility.ActiveOnDemandMeetingCount = await _breezyCosmosService.GetItem<int>(breezyContainers.OnDemandMeetings, queryDef);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(facility);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Returns the Microsoft Graph Team id associated with the provided channel.
        /// </summary>
        [OpenApiOperation(operationId: "channels-channelid-team-get", Summary = "/channels/{msChannelId}/team - GET", Description = "Retrieves the Microsoft Graph Team id associated with a channel")]
        [OpenApiParameter(name: "msChannelId", Description = "The id of the Microsoft Graph Teams channel", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Retrieved OK", Description = "Successfully retrieved the Microsoft Graph Team id")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The channel id does not exist")]
        [RequiredPermission("channels.ReadAll", "channels.ReadWriteAll")]
        [Function("TeamId_GetByChannelId")]
        public async Task<HttpResponseData> TeamIdGetByChannelId([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "channels/{msChannelId}/team")] HttpRequestData req, FunctionContext executionContext, string msChannelId)
        {
            var functionName = nameof(TeamIdGetByChannelId);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var queryDef = new QueryDefinition($"SELECT VALUE f.team.msTeamId FROM f join c in f.team.channels where c.msChannelId = @msChannelId")
                    .WithParameter("@msChannelId", msChannelId);
                var teamId = await _breezyCosmosService.GetItem<string>(breezyContainers.Facilities, queryDef);
                if (teamId == null) return await req.NotFoundResponse(logger, $"There are no teams associated with channel id '{msChannelId}'");

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                await retrievedResponse.WriteStringAsync(teamId);
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
