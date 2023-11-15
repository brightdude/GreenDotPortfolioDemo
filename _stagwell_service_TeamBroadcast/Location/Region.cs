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
    public class Region
    {
        private readonly LocationService _locationService; //TODO: This need refactoring 
       
        public Region(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _locationService = new LocationService(breezyCosmosService, authService);
        }

        /// <summary>
        /// Regions - retrieve all
        /// </summary>
        /// <returns>A list of all regions with active status</returns>
        [OpenApiOperation(operationId: "regions-list-all", Summary = "/regions - GET", Description = "Gets a list of all regions across the hierarchy")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RegionLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of regions")]
        [RequiredPermission("regions.ReadAll", "regions.ReadWriteAll")]
        [Function("Region_RetrieveAll")]
        public Task<HttpResponseData> RegionRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "regions")] HttpRequestData req, FunctionContext executionContext)
        {
            return _locationService.LocationRetrieveAll<RegionLocation>(req, executionContext, nameof(RegionRetrieveAll));
        }

        /// <summary>
        /// Regions - retrieve all by state
        /// </summary>
        /// <returns>A list of all regions for a state with active status</returns>
        [OpenApiOperation(operationId: "regions-list", Summary = "/countries/{countryId}/states/{stateId}/regions - GET", Description = "Gets a list of all active regions for the state")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RegionLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of regions")]
        [RequiredPermission("regions.ReadAll", "regions.ReadWriteAll")]
        [Function("Region_RetrieveByState")]
        public Task<HttpResponseData> RegionRetrieveByState([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions")] HttpRequestData req, FunctionContext executionContext, string stateId)
        {
            return _locationService.LocationRetrieveAll<RegionLocation>(req, executionContext, nameof(RegionRetrieveByState), stateId);
        }

        /// <summary>
        /// Regions - retrieve
        /// </summary>
        /// <returns>A region with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "regions-get", Summary = "/countries/{countryId}/states/{stateId}/regions/{regionId} - GET", Description = "Gets a specific region")]
        [OpenApiParameter(name: "id", Description = "The id of the region location to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RegionLocation), Summary = "Retrieved OK", Description = "Successfully retrieved the region")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The region id does not exist")]
        [RequiredPermission("regions.ReadAll", "regions.ReadWriteAll")]
        [Function("Region_Retrieve")]
        public Task<HttpResponseData> RegionRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{stateId}/regions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string id)
        {
            return _locationService.LocationRetrieve<RegionLocation>(req, executionContext, nameof(RegionRetrieve), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(id), id } });
        }

        /// <summary>
        /// Regions - create
        /// </summary>
        /// <returns>The created region object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "regions-post", Summary = "/countries/{countryId}/states/{stateId}/regions - POST", Description = "Adds a new region location")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegionLocationParams), Description = "The region location to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(RegionLocation), Summary = "Created OK", Description = "Region location successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The region id already exists")]
        [RequiredPermission("regions.ReadWriteAll")]
        [Function("Region_Create")]
        public async Task<HttpResponseData> RegionCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "countries/{countryId}/states/{stateId}/regions")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId)
        {
            return await _locationService.LocationInsert<RegionLocation, RegionLocationParams>(req, executionContext, nameof(RegionCreate), new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId } });
        }

        /// <summary>
        /// Regions - update
        /// </summary>
        /// <returns>The updated region object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "regions-patch", Summary = "/countries/{countryId}/states/{stateId}/regions - PATCH", Description = "Updates an existing region location")]
        [OpenApiParameter(name: "id", Description = "The id of the region location to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegionLocationParams), Description = "The region location to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(RegionLocation), Summary = "Updated OK", Description = "Region location successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The region id does not exist")]
        [RequiredPermission("regions.ReadWriteAll")]
        [Function("Region_Update")]
        public async Task<HttpResponseData> RegionUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "countries/{countryId}/states/{stateId}/regions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string id)
        {
            return await _locationService.LocationUpdate<RegionLocation, RegionLocationParams>(req, executionContext, nameof(RegionUpdate), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(id), id } });
        }

        /// <summary>
        /// Regions - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "regions-delete", Summary = "/countries/{countryId}/states/{stateId}/regions - DELETE", Description = "Deletes an existing region location")]
        [OpenApiParameter(name: "id", Description = "The id of the region location to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "stateId", Description = "The id of the state associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the region", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Region location successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The region id does not exist")]
        [RequiredPermission("regions.ReadWriteAll")]
        [Function("Region_Delete")]
        public async Task<HttpResponseData> RegionDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "countries/{countryId}/states/{stateId}/regions/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string stateId, string id)
        {
            return await _locationService.LocationDelete<RegionLocation>(req, executionContext, nameof(RegionDelete), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(stateId), stateId }, { nameof(id), id } });
        }
    }
}
