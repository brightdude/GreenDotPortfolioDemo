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
    public class SubRegion
    {
        private readonly LocationService _locationService; //TODO: This need refactoring       

        public SubRegion(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _locationService = new LocationService(breezyCosmosService, authService);
        }

        /// <summary>
        /// SubRegions - retrieve all
        /// </summary>
        /// <returns>A list of all subregions with active status</returns>
        [OpenApiOperation(operationId: "supregion-list-all", Summary = "/subRegions - GET", Description = "Gets a list of all active subregions across the hierarchy")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RegionLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of regions")]
        [RequiredPermission("subregions.ReadAll", "subregions.ReadWriteAll")]
        [Function("SubRegion_RetrieveAll")]
        public Task<HttpResponseData> SubRegionRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subregions")] HttpRequestData req, FunctionContext executionContext)
        {
            return _locationService.LocationRetrieveAll<SubRegionLocation>(req, executionContext, nameof(SubRegionRetrieveAll));
        }
        /// <summary>
        /// SubRegions - retrieve all by region
        /// </summary>
        /// <returns>A list of all sub regions with active status for a region</returns>
        [OpenApiOperation(operationId: "subregions-list-byregion", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions - GET", Description = "Gets a list of all active subregions for a region")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SubRegionLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of subregions")]
        [RequiredPermission("subregions.ReadAll", "subregions.ReadWriteAll")]
        [Function("SubRegion_RetrieveByRegion")]
        public Task<HttpResponseData> SubRegionRetrieveByRegion([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions")] HttpRequestData req, FunctionContext executionContext, string regionId)
        {
            return _locationService.LocationRetrieveAll<SubRegionLocation>(req, executionContext, nameof(SubRegionRetrieveByRegion), regionId);
        }

        /// <summary>
        /// SubRegions - retrieve
        /// </summary>
        /// <returns>A subregion with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "subregions-get", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions/{subRegionId} - GET", Description = "Gets a specific subregion")]
        [OpenApiParameter(name: "id", Description = "The id of the subregion location to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SubRegionLocation), Summary = "Retrieved OK", Description = "Successfully retrieved the subregion")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The subregion id does not exist")]
        [RequiredPermission("subregions.ReadAll", "subregions.ReadWriteAll")]
        [Function("SubRegion_Retrieve")]
        public Task<HttpResponseData> SubRegionRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string id)
        {
            return _locationService.LocationRetrieve<SubRegionLocation>(req, executionContext, nameof(SubRegionRetrieve), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(id), id } });
        }

        /// <summary>
        /// SubRegions - create
        /// </summary>
        /// <returns>The created subregion object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "subregions-post", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions - POST", Description = "Adds a new subregion location")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SubRegionLocationParams), Description = "The subregion location to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(SubRegionLocation), Summary = "Created OK", Description = "Subregion location successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The subregion id already exists")]
        [RequiredPermission("subregions.ReadWriteAll")]
        [Function("SubRegion_Create")]
        public async Task<HttpResponseData> SubRegionCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions")] HttpRequestData req, FunctionContext executionContext, string countryId, string regionId, string stateId)
        {
            return await _locationService.LocationInsert<SubRegionLocation, SubRegionLocationParams>(req, executionContext, nameof(SubRegionCreate), new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId } });
        }

        /// <summary>
        /// SubRegions - update
        /// </summary>
        /// <returns>The updated subregion object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "subregions-patch", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions - PATCH", Description = "Updates an existing subregion location")]
        [OpenApiParameter(name: "id", Description = "The id of the subregion location to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SubRegionLocationParams), Description = "The subregion location to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(SubRegionLocation), Summary = "Updated OK", Description = "Subregion location successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The subregion id does not exist")]
        [RequiredPermission("subregions.ReadWriteAll")]
        [Function("SubRegion_Update")]
        public async Task<HttpResponseData> SubRegionUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string id)
        {
            return await _locationService.LocationUpdate<SubRegionLocation, SubRegionLocationParams>(req, executionContext, nameof(SubRegionUpdate), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(id), id } });
        }

        /// <summary>
        /// SubRegions - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "subregions-delete", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions - DELETE", Description = "Deletes an existing subregion location")]
        [OpenApiParameter(name: "id", Description = "The id of the subregion location to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the subRegion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "regionId", Description = "The id of the region associated with the subregion", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Subregion location successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The subregion id does not exist")]
        [RequiredPermission("subregions.ReadWriteAll")]
        [Function("SubRegion_Delete")]
        public async Task<HttpResponseData> SubRegionDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "countries/{countryId}/states/{stateId}/regions/{regionId}/subRegions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string regionId, string id)
        {
            return await _locationService.LocationDelete<SubRegionLocation>(req, executionContext, nameof(SubRegionDelete), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(regionId), regionId }, { nameof(id), id } });
        }
    }
}
