using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster.Lookup
{
    public class PersonnelRole
    {
        private readonly LookupService _lookupService; //TODO: This need refactoring       
                
        public PersonnelRole(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _lookupService = new LookupService(breezyCosmosService, authService);
        }

        /// <summary>
        /// PersonnelRoles - retrieve all
        /// </summary>
        /// <returns>A list of all personnel roles with active status</returns>
        [OpenApiOperation(operationId: "personnelroles-list", Summary = "/personnelRoles - GET", Description = "Gets a list of all personnel roles")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of personnel roles")]
        [RequiredPermission("personnelroles.ReadAll", "personnelroles.ReadWriteAll")]
        [Function("PersonnelRole_RetrieveAll")]
        public Task<HttpResponseData> PersonnelRoleRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "personnelRoles")] HttpRequestData req, FunctionContext executionContext)
        {
            return _lookupService.LookupRetrieveAll(req, executionContext, "PersonnelRoles", nameof(PersonnelRoleRetrieveAll));
        }

        /// <summary>
        /// PersonnelRoles - retrieve
        /// </summary>
        /// <returns>A personnel role with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "personnelroles-get", Summary = "/personnelRoles/{personnelRoleId} - GET", Description = "Gets a personnel role with the specified id")]
        [OpenApiParameter(name: "personnelRoleId", Description = "The id of the personnel role to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Retrieved OK", Description = "Successfully retrieved the personnel role")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The personnel role id does not exist")]
        [RequiredPermission("personnelroles.ReadAll", "personnelroles.ReadWriteAll")]
        [Function("PersonnelRole_Retrieve")]
        public Task<HttpResponseData> PersonnelRoleRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "personnelRoles/{personnelRoleId}")] HttpRequestData req, FunctionContext executionContext, string personnelRoleId)
        {
            return _lookupService.LookupRetrieve(req, executionContext, "PersonnelRoles", nameof(PersonnelRoleRetrieve), personnelRoleId);
        }

        /// <summary>
        /// PersonnelRoles - create
        /// </summary>
        /// <returns>The created personnel role object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "personnelroles-post", Summary = "/personnelRoles - POST", Description = "Adds a new personnel role")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The personnel role to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Created OK", Description = "Personnel Role successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The personnel role id already exists")]
        [RequiredPermission("personnelroles.ReadWriteAll")]
        [Function("PersonnelRole_Create")]
        public async Task<HttpResponseData> PersonnelRoleCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "personnelRoles")] HttpRequestData req, FunctionContext executionContext)
        {
            return await _lookupService.LookupInsert(req, executionContext, "PersonnelRoles", nameof(PersonnelRoleCreate));
        }

        /// <summary>
        /// PersonnelRoles - update
        /// </summary>
        /// <returns>The updated personnel role object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "personnelroles-patch", Summary = "/personnelRoles - PATCH", Description = "Updates an existing personnel role")]
        [OpenApiParameter(name: "personnelRoleId", Description = "The id of the personnel role to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The personnel role to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Updated OK", Description = "Personnel Role successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The personnel role id does not exist")]
        [RequiredPermission("personnelroles.ReadWriteAll")]
        [Function("PersonnelRole_Update")]
        public async Task<HttpResponseData> PersonnelRoleUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "personnelRoles/{personnelRoleId}")] HttpRequestData req, FunctionContext executionContext, string personnelRoleId)
        {
            return await _lookupService.LookupUpdate(req, executionContext, "PersonnelRoles", nameof(PersonnelRoleUpdate), personnelRoleId);
        }

        /// <summary>
        /// PersonnelRoles - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "personnelroles-delete", Summary = "/personnelRoles - DELETE", Description = "Deletes an existing personnel role")]
        [OpenApiParameter(name: "personnelRoleId", Description = "The id of the personnel role to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "PersonnelRole successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The personnel role id does not exist")]
        [RequiredPermission("personnelroles.ReadWriteAll")]
        [Function("PersonnelRole_Delete")]
        public async Task<HttpResponseData> PersonnelRoleDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "personnelRoles/{personnelRoleId}")] HttpRequestData req, FunctionContext executionContext, string personnelRoleId)
        {
            return await _lookupService.LookupDelete(req, executionContext, "PersonnelRoles", nameof(PersonnelRoleDelete), personnelRoleId);
        }
    }
}
