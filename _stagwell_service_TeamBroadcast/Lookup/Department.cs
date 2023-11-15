using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster.Lookup
{
    public class Department
    {
        private readonly LookupService _lookupService; //TODO: This need refactoring       

        public Department(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _lookupService = new LookupService(breezyCosmosService, authService);
        }

        /// <summary>
        /// Departments - retrieve all
        /// </summary>
        /// <returns>A list of all departments with active status</returns>
        [OpenApiOperation(operationId: "departments-list", Summary = "/departments - GET", Description = "Gets a list of all departments")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of departments")]
        [RequiredPermission("departments.ReadAll", "departments.ReadWriteAll")]
        [Function("Department_RetrieveAll")]
        public Task<HttpResponseData> DepartmentRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments")] HttpRequestData req, FunctionContext executionContext)
        {
            return _lookupService.LookupRetrieveAll(req, executionContext, "Departments", nameof(DepartmentRetrieveAll));
        }

        /// <summary>
        /// Departments - retrieve
        /// </summary>
        /// <returns>A department with the specified id, or 404 if not found</returns>
        [OpenApiOperation(operationId: "departments-get", Summary = "/departments/{departmentId} - GET", Description = "Gets a department with the specified id")]
        [OpenApiParameter(name: "departmentId", Description = "The id of the department to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Retrieved OK", Description = "Successfully retrieved the department")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The department id does not exist")]
        [RequiredPermission("departments.ReadAll", "departments.ReadWriteAll")]
        [Function("Department_Retrieve")]
        public Task<HttpResponseData> DepartmentRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments/{departmentId}")] HttpRequestData req, FunctionContext executionContext, string departmentId)
        {
            return _lookupService.LookupRetrieve(req, executionContext, "Departments", nameof(DepartmentRetrieve), departmentId);
        }

        /// <summary>
        /// Departments - create
        /// </summary>
        /// <returns>The created department object, or 409 if it already exists</returns>
        [OpenApiOperation(operationId: "departments-post", Summary = "/departments - POST", Description = "Adds a new department")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The department to add", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Created OK", Description = "Department successfully created")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The department id already exists")]
        [RequiredPermission("departments.ReadWriteAll")]
        [Function("Department_Create")]
        public async Task<HttpResponseData> DepartmentCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "departments")] HttpRequestData req, FunctionContext executionContext)
        {
            return await _lookupService.LookupInsert(req, executionContext, "Departments", nameof(DepartmentCreate));
        }

        /// <summary>
        /// Departments - update
        /// </summary>
        /// <returns>The updated department object, or 404 if not found</returns>
        [OpenApiOperation(operationId: "departments-patch", Summary = "/departments - PATCH", Description = "Updates an existing department")]
        [OpenApiParameter(name: "departmentId", Description = "The id of the department to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LookupItem), Description = "The department to update", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LookupItem), Summary = "Updated OK", Description = "Department successfully updated")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "The request body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The department id does not exist")]
        [RequiredPermission("departments.ReadWriteAll")]
        [Function("Department_Update")]
        public async Task<HttpResponseData> DepartmentUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "departments/{departmentId}")] HttpRequestData req, FunctionContext executionContext, string departmentId)
        {
            return await _lookupService.LookupUpdate(req, executionContext, "Departments", nameof(DepartmentUpdate), departmentId);
        }

        /// <summary>
        /// Departments - delete
        /// </summary>
        /// <returns>204 if successfully deleted, or 404 if not found</returns>
        [OpenApiOperation(operationId: "departments-delete", Summary = "/departments - DELETE", Description = "Deletes an existing department")]
        [OpenApiParameter(name: "departmentId", Description = "The id of the department to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Department successfully deleted")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The department id does not exist")]
        [RequiredPermission("departments.ReadWriteAll")]
        [Function("Department_Delete")]
        public async Task<HttpResponseData> DepartmentDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "departments/{departmentId}")] HttpRequestData req, FunctionContext executionContext, string departmentId)
        {
            return await _lookupService.LookupDelete(req, executionContext, "Departments", nameof(DepartmentDelete), departmentId);
        }
    }
}
