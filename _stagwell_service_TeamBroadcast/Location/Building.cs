using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using static Breezy.Muticaster.LocationService;

namespace Breezy.Muticaster.Location
{
    public class Building
    {       
        private readonly LocationService _locationService; //TODO: This need refactoring         

        public Building(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _locationService = new LocationService(breezyCosmosService, authService);
        }

        /// <summary>
        /// Buildings - retrieve all
        /// </summary>
        /// <returns>A list of all buildings with active status</returns>
        [OpenApiOperation(operationId: "buildings-list-all", Summary = "/buildings - GET", Description = "Gets a list of all buildings across the hierarchy")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BuildingLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of buildings")]
        [RequiredPermission("buildings.ReadAll", "buildings.ReadWriteAll")]
        [Function("Building_RetrieveAll")]
        public Task<HttpResponseData> BuildingRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings")] HttpRequestData req, FunctionContext executionContext)
        {
            return _locationService.LocationRetrieveAll<BuildingLocation>(req, executionContext, nameof(BuildingRetrieveAll));
        }

        /// <summary>
        /// Buildings - retrieve all by subregion
        /// </summary>
        /// <returns>A list of all buildings with active status for a subregion</returns>
        [OpenApiOperation(operationId: "buildings-list-bysubregion", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings - GET", Description = "Gets a list of all active buildings for a subregion")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "subRegionId", Description = "The id of the subregion associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BuildingLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of buildings")]
        [RequiredPermission("buildings.ReadAll", "buildings.ReadWriteAll")]
        [Function("Building_RetrieveBySubRegion")]
        public Task<HttpResponseData> BuildingRetrieveBySubRegion([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings")] HttpRequestData req, FunctionContext executionContext, string subRegionId)
        {
            return _locationService.LocationRetrieveAll<BuildingLocation>(req, executionContext, nameof(BuildingRetrieveBySubRegion), subRegionId);
        }

        /// <summary>
        /// Buildings - retrieve
        /// </summary>
        /// <returns>A building with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "buildings-get", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings/{buildingId} - GET", Description = "Gets a specific building")]
        [OpenApiParameter(name: "id", Description = "The id of the building location to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "subRegionId", Description = "The id of the subregion associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BuildingLocation), Summary = "Retrieved OK", Description = "Successfully retrieved the building")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The building id does not exist")]
        [RequiredPermission("buildings.ReadAll", "buildings.ReadWriteAll")]
        [Function("Building_Retrieve")]
        public Task<HttpResponseData> BuildingRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string subRegionId, string id)
        {
            return _locationService.LocationRetrieve<BuildingLocation>(req, executionContext, nameof(BuildingRetrieve), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(subRegionId), subRegionId }, { nameof(id), id } });
        }

        /// <summary>
        /// Buildings - create
        /// </summary>
        /// <returns>The created building object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "buildings-post", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings - POST", Description = "Adds a new building location")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "subRegionId", Description = "The id of the subregion associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(BuildingLocationParams), Description = "The building location to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(BuildingLocation), Summary = "Created OK", Description = "Building location successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The building id already exists")]
        [RequiredPermission("buildings.ReadWriteAll")]
        [Function("Building_Create")]
        public async Task<HttpResponseData> BuildingCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string subRegionId)
        {
            return await _locationService.LocationInsert<BuildingLocation, BuildingLocationParams>(req, executionContext, nameof(BuildingCreate), new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(subRegionId), subRegionId } });
        }

        /// <summary>
        /// Buildings - update
        /// </summary>
        /// <returns>The updated building object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "buildings-patch", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings - PATCH", Description = "Updates an existing building location")]
        [OpenApiParameter(name: "id", Description = "The  of the building location to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "subRegionId", Description = "The id of the subregion associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(BuildingLocationParams), Description = "The building location to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(BuildingLocation), Summary = "Updated OK", Description = "Building location successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The building id does not exist")]
        [RequiredPermission("buildings.ReadWriteAll")]
        [Function("Building_Update")]
        public async Task<HttpResponseData> BuildingUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string subRegionId, string id)
        {
            return await _locationService.LocationUpdate<BuildingLocation, BuildingLocationParams>(req, executionContext, nameof(BuildingUpdate), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(subRegionId), subRegionId }, { nameof(id), id } });
        }

        /// <summary>
        /// Buildings - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "buildings-delete", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings - DELETE", Description = "Deletes an existing building location")]
        [OpenApiParameter(name: "id", Description = "The id of the building location to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "subRegionId", Description = "The id of the subregion associated with the building", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Subregion location successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The building id does not exist")]
        [RequiredPermission("buildings.ReadWriteAll")]
        [Function("Building_Delete")]
        public async Task<HttpResponseData> BuildingDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subregions/{subRegionId}/buildings/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string subRegionId, string id)
        {
            return await _locationService.LocationDelete<BuildingLocation>(req, executionContext, nameof(BuildingDelete), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(subRegionId), subRegionId }, { nameof(id), id } });
        }
    }
}
