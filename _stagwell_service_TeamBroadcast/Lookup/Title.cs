using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster.Lookup
{
    internal class Title
    {
        private readonly LookupService _lookupService; //TODO: This need refactoring 
        
        public Title(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _lookupService = new LookupService(breezyCosmosService, authService);
        }

        /// <summary>
        /// Titles - retrieve all
        /// </summary>
        /// <returns>A list of all titles with active status</returns>
        [OpenApiOperation(operationId: "titles-list", Summary = "/titles - GET", Description = "Gets a list of all titles")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of titles")]
        [RequiredPermission("titles.ReadAll", "titles.ReadWriteAll")]
        [Function("Title_RetrieveAll")]
        public Task<HttpResponseData> TitleRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "titles")] HttpRequestData req, FunctionContext executionContext)
        {
            return _lookupService.LookupRetrieveAll(req, executionContext, "Titles", nameof(TitleRetrieveAll));
        }

        /// <summary>
        /// Titles - retrieve
        /// </summary>
        /// <returns>A title with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "titles-get", Summary = "/titles/{titleId} - GET", Description = "Gets a title with the specified id")]
        [OpenApiParameter(name: "titleId", Description = "The id of the title to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Retrieved OK", Description = "Successfully retrieved the title")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The title id does not exist")]
        [RequiredPermission("titles.ReadAll", "titles.ReadWriteAll")]
        [Function("Title_Retrieve")]
        public Task<HttpResponseData> TitleRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "titles/{titleId}")] HttpRequestData req, FunctionContext executionContext, string titleId)
        {
            return _lookupService.LookupRetrieve(req, executionContext, "Titles", nameof(TitleRetrieve), titleId);
        }

        /// <summary>
        /// Titles - create
        /// </summary>
        /// <returns>The created title object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "titles-post", Summary = "/titles - POST", Description = "Adds a new title")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The title to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Created OK", Description = "Title successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The title id already exists")]
        [RequiredPermission("titles.ReadWriteAll")]
        [Function("Title_Create")]
        public async Task<HttpResponseData> TitleCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "titles")] HttpRequestData req, FunctionContext executionContext)
        {
            return await _lookupService.LookupInsert(req, executionContext, "Titles", nameof(TitleCreate));
        }

        /// <summary>
        /// Titles - update
        /// </summary>
        /// <returns>The updated title object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "titles-patch", Summary = "/titles - PATCH", Description = "Updates an existing title")]
        [OpenApiParameter(name: "titleId", Description = "The id of the title to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The title to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Updated OK", Description = "Title successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The title id does not exist")]
        [RequiredPermission("titles.ReadWriteAll")]
        [Function("Title_Update")]
        public async Task<HttpResponseData> TitleUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "titles/{titleId}")] HttpRequestData req, FunctionContext executionContext, string titleId)
        {
            return await _lookupService.LookupUpdate(req, executionContext, "Titles", nameof(TitleUpdate), titleId);
        }

        /// <summary>
        /// Titles - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "titles-delete", Summary = "/titles - DELETE", Description = "Deletes an existing title")]
        [OpenApiParameter(name: "titleId", Description = "The id of the title to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Title successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The title id does not exist")]
        [RequiredPermission("titles.ReadWriteAll")]
        [Function("Title_Delete")]
        public async Task<HttpResponseData> TitleDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "titles/{titleId}")] HttpRequestData req, FunctionContext executionContext, string titleId)
        {
            return await _lookupService.LookupDelete(req, executionContext, "Titles", nameof(TitleDelete), titleId);
        }
    }
}
