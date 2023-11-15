using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class User
    {
        /// <summary>
        /// Returns a list of all active Users.
        /// </summary>
        [OpenApiOperation(operationId: "users-get", Summary = "/users - GET", Description = "Retrieves a list of active users")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(User[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of users")]
        [RequiredPermission("users.ReadAll", "users.ReadWriteAll")]
        [Function("User_RetrieveAll")]
        public async Task<HttpResponseData> UserRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(UserRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var users = await _breezyCosmosService.ListUsers(true);
                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(users);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Returns a User with the specified email address.
        /// </summary>
        [OpenApiOperation(operationId: "users-emailaddress-get", Summary = "/users/{emailAddress} - GET", Description = "Retrieves a user with the specified email address")]
        [OpenApiParameter(name: "emailAddress", Description = "The email address of the user to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(User), Summary = "Retrieved OK", Description = "Successfully retrieved the user")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The user was not found")]
        [RequiredPermission("users.ReadAll", "users.ReadWriteAll")]
        [Function("User_Retrieve")]
        public async Task<HttpResponseData> UserRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{emailAddress}")] HttpRequestData req, FunctionContext executionContext, string emailAddress)
        {
            var functionName = nameof(UserRetrieve);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var user = await _breezyCosmosService.GetUser(emailAddress);
                if (user == null)
                {
                    return await req.NotFoundResponse(logger, $"A user with email address '{emailAddress.ToLower()}' was not found");
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(user);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
