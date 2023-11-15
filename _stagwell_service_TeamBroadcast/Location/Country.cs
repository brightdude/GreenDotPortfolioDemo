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
    public class Country
    {
        private readonly LocationService _locationService; //TODO: This need refactoring
        public Country(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _locationService = new LocationService(breezyCosmosService, authService);
        }

        /// <summary>
        /// Countries - retrieve all
        /// </summary>
        /// <returns>A list of all countries with active status</returns>
        [OpenApiOperation(operationId: "countries-list", Summary = "/countries - GET", Description = "Gets a list of all countries")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CountryLocation[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of countries")]
        [RequiredPermission("countries.ReadAll", "countries.ReadWriteAll")]
        [Function("Country_RetrieveAll")]
        public async Task<HttpResponseData> CountryRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries")] HttpRequestData req, FunctionContext executionContext)
        {
            return await _locationService.LocationRetrieveAll<CountryLocation>(req, executionContext, nameof(CountryRetrieveAll));
        }

        /// <summary>
        /// Countries - retrieve
        /// </summary>
        /// <returns>A country with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "countries-get", Summary = "/countries/{id} - GET", Description = "Gets a specific country")]
        [OpenApiParameter(name: "id", Description = "The id of the country location to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CountryLocation), Summary = "Retrieved OK", Description = "Successfully retrieved the country")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The country id does not exist")]
        [RequiredPermission("countries.ReadAll", "countries.ReadWriteAll")]
        [Function("Country_Retrieve")]
        public Task<HttpResponseData> CountryRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{id}")] HttpRequestData req, FunctionContext executionContext, string id)
        {
            return _locationService.LocationRetrieve<CountryLocation>(req, executionContext, nameof(CountryRetrieve), id, new Dictionary<string, string> { { nameof(id), id } });
        }

        /// <summary>
        /// Countries - create
        /// </summary>
        /// <returns>The created country object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "countries-post", Summary = "/countries - POST", Description = "Adds a new country location")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CountryLocationParams), Description = "The country location to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(CountryLocation), Summary = "Created OK", Description = "Country location successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The country id already exists")]
        [RequiredPermission("countries.ReadWriteAll")]
        [Function("Country_Create")]
        public async Task<HttpResponseData> CountryCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "countries")] HttpRequestData req, FunctionContext executionContext)
        {
            return await _locationService.LocationInsert<CountryLocation, CountryLocationParams>(req, executionContext, nameof(CountryCreate), new Dictionary<string, string>());
        }

        /// <summary>
        /// Countries - update
        /// </summary>
        /// <returns>The updated country object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "countries-patch", Summary = "/countries - PATCH", Description = "Updates an existing country location")]
        [OpenApiParameter(name: "id", Description = "The id of the country location to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CountryLocationParams), Description = "The country location to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(CountryLocation), Summary = "Updated OK", Description = "Country location successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The country id does not exist")]
        [RequiredPermission("countries.ReadWriteAll")]
        [Function("Country_Update")]
        public async Task<HttpResponseData> CountryUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "countries/{id}")] HttpRequestData req, FunctionContext executionContext, string id)
        {
            return await _locationService.LocationUpdate<CountryLocation, CountryLocationParams>(req, executionContext, nameof(CountryUpdate), id, new Dictionary<string, string> { { nameof(id), id } });
        }

        /// <summary>
        /// Countries - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "countries-delete", Summary = "/countries - DELETE", Description = "Deletes an existing country location")]
        [OpenApiParameter(name: "id", Description = "The id of the country location to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Country location successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The country id does not exist")]
        [RequiredPermission("countries.ReadWriteAll")]
        [Function("Country_Delete")]
        public async Task<HttpResponseData> CountryDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "countries/{id}")] HttpRequestData req, FunctionContext executionContext, string id)
        {
            return await _locationService.LocationDelete<CountryLocation>(req, executionContext, nameof(CountryDelete), id, new Dictionary<string, string> { { nameof(id), id } });
        }
    }
}
