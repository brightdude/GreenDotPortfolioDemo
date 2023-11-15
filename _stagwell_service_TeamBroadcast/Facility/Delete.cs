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
        [OpenApiOperation(operationId: "facilities-delete", Summary = "/facilities - DELETE", Description = "Deletes a facility")]
        [OpenApiParameter(name: "facilityId", Description = "The unique identifier for the facility", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Delete Successful", Description = "Successfully removed facility")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The facility id was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("facilities.ReadWriteAll")]
        [Function("Facility_Delete")]
        public async Task<HttpResponseData> FacilityDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "facilities/{facilityId}")] HttpRequestData req, FunctionContext executionContext, string facilityId)
        {
            var functionName = nameof(FacilityDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get facility
                var facility = await _breezyCosmosService.GetFacility(facilityId);
                if (facility == null) return await req.NotFoundResponse( logger, $"Could not find a facility with id '{facilityId.ToLower()}'");

                // Check for related calendars
                var calendars = await _breezyCosmosService.ListCalendars(facility.Id);
                if (calendars.Any()) return await req.ConflictResponse(logger, $"Unable to delete facility '{facility.Id}' because it is linked to the following calendars: {string.Join(", ", calendars.Select(c => c.ExternalCalendarId))}.");

                // Delete the associated team                
                await _graphTeamsService.Delete(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId);

                // Delete the facility document
                await _breezyCosmosService.DeleteItem<Facility>(breezyContainers.Facilities, facility.Id, facility.Id);

                // Find any associated on-demand meetings
                var onDemandMeetings = await _breezyCosmosService.ListCalendarsForTeam(facility.Team.MsTeamId);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Internal Server Error");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal Server Error");
                return errorResponse;
            }
        }
    }
}