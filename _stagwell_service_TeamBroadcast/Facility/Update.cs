using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class Facility
    {
        [OpenApiOperation(operationId: "facilities-facilityId-patch", Summary = "/facilities/{facilityId} - PATCH", Description = "Updates an existing facility")]
        [OpenApiParameter(name: "facilityId", Description = "The unique identifier for the facility", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FacilityCreateUpdateParams), Required = true, Description = "The facility to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Event), Summary = "Updated", Description = "The facility was updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The facility does not exist")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("facilities.ReadWriteAll")]
        [Function("Facility_Update")]
        public async Task<HttpResponseData> FacilityUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "facilities/{facilityId}")] HttpRequestData req, FunctionContext executionContext, string facilityId)
        {
            var functionName = nameof(FacilityUpdate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<FacilityCreateUpdateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                var id = facilityId.ToLower();
                if (id != input.Id.ToLower()) return await req.BadRequestResponse(logger, $"Facility id supplied in url '{id}' does not match id supplied in document '{input.Id}'.");

                // Check facility exists
                var facility = await _breezyCosmosService.GetFacility(id);
                if (facility == null) return await req.NotFoundResponse(logger, $"A facility with id '{id}' was not found");

                // Check building exists
                var buildingId = input.BuildingId.ToLower();
                if (facility.BuildingId.ToLower() != buildingId)
                {
                    // Building has changed - get its details and apply them to the facility
                    var building = await _breezyCosmosService.GetItem<LocationService.BuildingLocation>(breezyContainers.Locations, buildingId, buildingId);
                    if (building == null || building.Status != "Active") return await req.NotFoundResponse(logger, $"A building with id '{input.BuildingId.ToLower()}' was not found");

                    facility.BuildingId = building.Id;
                    facility.BuildingName = building.Name;
                    facility.SubRegionId = building.SubRegionId;
                    facility.SubRegionName = building.SubRegionName;
                    facility.RegionId = building.RegionId;
                    facility.RegionName = building.RegionName;
                    facility.StateId = building.StateId;
                    facility.StateName = building.StateName;
                    facility.CountryId = building.CountryId;
                    facility.CountryName = building.CountryName;
                }

                facility.FacilityType = input.FacilityType;
                facility.Room = input.Room;
                facility.Floor = input.Floor;

                var displayName = !string.IsNullOrWhiteSpace(input.DisplayName) ? input.DisplayName : BuildFacilityName(facility.RegionName, facility.BuildingName, facility.Room);
                if (facility.DisplayName != displayName)
                {
                    // If the supplied name (or default if none was supplied) has changed, update the team name in graph
                    logger.LogInformation("Updating display name of facility '{facilityId}' and associated team to '{displayName}'", facility.Id, displayName);
                    await MsTeamUpdate(facility.Team.MsTeamId, displayName, logger);
                    facility.DisplayName = displayName;
                    facility.Team.Name = displayName;
                }

                var updateResponse = await _breezyCosmosService.UpsertItem(breezyContainers.Facilities, facility, facility.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                var updatedResponse = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JObject.FromObject(updateResponse.Resource);
                obj.Property("team").Remove();
                await updatedResponse.WriteStringAsync(obj.ToString());
                return updatedResponse;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Internal Server Error");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal Server Error");
                return errorResponse;
            }
        }

        private async Task<Microsoft.Graph.Team> MsTeamUpdate(string teamId, string displayName, ILogger logger)
        {           
            return await _graphTeamsService.Update(AuthenticationType.ScheduledEventService, teamId, displayName);
        }
    }
}
