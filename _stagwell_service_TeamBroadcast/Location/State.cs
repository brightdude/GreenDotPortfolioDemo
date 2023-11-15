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
    public class State
    {
        private readonly LocationService _locationService; //TODO: This need refactoring
        
        public State(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _locationService = new LocationService(breezyCosmosService, authService);
        }

        /// <summary>
        /// States - retrieve all
        /// </summary>
        /// <returns>A list of all states with active status</returns>
        [OpenApiOperation(operationId: "states-list-all", Summary = "/states - GET", Description = "Gets a list of all states across the hierarchy")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StateLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of states")]
        [RequiredPermission("states.ReadAll", "states.ReadWriteAll")]
        [Function("State_RetrieveAll")]
        public Task<HttpResponseData> StateRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "states")] HttpRequestData req, FunctionContext executionContext)
        {
            return _locationService.LocationRetrieveAll<StateLocation>(req, executionContext, nameof(StateRetrieveAll));
        }

        /// <summary>
        /// States - retrieve all
        /// </summary>
        /// <returns>A list of all states in a country with active status</returns>
        [OpenApiOperation(operationId: "states-list", Summary = "/countries/{countryId}/states - GET", Description = "Gets a list of all active states in a country")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the state", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StateLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of states")]
        [RequiredPermission("states.ReadAll", "states.ReadWriteAll")]
        [Function("State_RetrieveByCountry")]
        public Task<HttpResponseData> StateRetrieveByCountry([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states")] HttpRequestData req, FunctionContext executionContext, string countryId)
        {
            return _locationService.LocationRetrieveAll<StateLocation>(req, executionContext, nameof(StateRetrieveByCountry), countryId);
        }

        /// <summary>
        /// States - retrieve
        /// </summary>
        /// <returns>A state with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "states-get", Summary = "/countries/{countryId}/states/{stateId} - GET", Description = "Gets a specific state")]
        [OpenApiParameter(name: "id", Description = "The id of the state location to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the state", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StateLocation), Summary = "Retrieved OK", Description = "Successfully retrieved the state")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The state id does not exist")]
        [RequiredPermission("states.ReadAll", "states.ReadWriteAll")]
        [Function("State_Retrieve")]
        public Task<HttpResponseData> StateRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{countryId}/states/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string id)
        {
            return _locationService.LocationRetrieve<StateLocation>(req, executionContext, nameof(StateRetrieve), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(id), id } });
        }

        /// <summary>
        /// States - create
        /// </summary>
        /// <returns>The created state object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "states-post", Summary = "/countries/{countryId}/states - POST", Description = "Adds a new state location")]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the state", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(StateLocationParams), Description = "The state location to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(StateLocation), Summary = "Created OK", Description = "State location successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The state id already exists")]
        [RequiredPermission("states.ReadWriteAll")]
        [Function("State_Create")]
        public async Task<HttpResponseData> StateCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "countries/{countryId}/states")] HttpRequestData req, FunctionContext executionContext, string countryId)
        {
            return await _locationService.LocationInsert<StateLocation, StateLocationParams>(req, executionContext, nameof(StateCreate), new Dictionary<string, string> { { nameof(countryId), countryId } });
        }

        /// <summary>
        /// States - update
        /// </summary>
        /// <returns>The updated state object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "states-patch", Summary = "/countries/{countryId}/states - PATCH", Description = "Updates an existing state location")]
        [OpenApiParameter(name: "id", Description = "The id of the state location to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the state", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(StateLocationParams), Description = "The state location to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(StateLocation), Summary = "Updated OK", Description = "State location successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The state id does not exist")]
        [RequiredPermission("states.ReadWriteAll")]
        [Function("State_Update")]
        public async Task<HttpResponseData> StateUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "countries/{countryId}/states/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string id)
        {
            return await _locationService.LocationUpdate<StateLocation, StateLocationParams>(req, executionContext, nameof(StateUpdate), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(id), id } });
        }

        /// <summary>
        /// States - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "states-delete", Summary = "/countries/{countryId}/states - DELETE", Description = "Deletes an existing state location")]
        [OpenApiParameter(name: "id", Description = "The id of the state location to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "countryId", Description = "The id of the country associated with the state", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "State location successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The state id does not exist")]
        [RequiredPermission("states.ReadWriteAll")]
        [Function("State_Delete")]
        public async Task<HttpResponseData> StateDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "countries/{countryId}/states/{id}")] HttpRequestData req, FunctionContext executionContext, string countryId, string id)
        {
            return await _locationService.LocationDelete<StateLocation>(req, executionContext, nameof(StateDelete), id, new Dictionary<string, string> { { nameof(countryId), countryId }, { nameof(id), id } });
        }
    }
}
