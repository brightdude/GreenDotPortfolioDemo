using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class User
    {
        [OpenApiOperation(operationId: "users-emailaddress-delete", Summary = "/users/{emailAddress} - DELETE", Description = "Deletes a user with the specified email address")]
        [OpenApiParameter(name: "emailAddress", Description = "The email address of the user to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Successfully deleted the user")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The user was not found")]
        [RequiredPermission("users.ReadWriteAll")]
        [Function("User_Delete")]
        public async Task<HttpResponseData> UserDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{emailAddress}")] HttpRequestData req, FunctionContext executionContext, string emailAddress)
        {
            var functionName = nameof(UserDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Check user exists and is active
                var user = await _breezyCosmosService.GetUser(emailAddress);
                if (user == null || !user.ActiveFlag)
                {
                    return await req.NotFoundResponse(logger, $"A user with email address '{emailAddress.ToLower()}' was not found");
                }

                // Check if user is linked to any calendars
                foreach (var calendar in await _breezyCosmosService.ListCalendarsWithfocusUser(emailAddress))
                {
                    // Remove user from channels
                    var result = await _calendarService.UserRemoveCalendarAssignment(logger, user.Email, calendar);
                    if (result.Item1 >= 200 && result.Item1 < 300)
                    {
                        // Unlink user from calendar
                        calendar.focusUsers = calendar.focusUsers.Where(c => c != user.Email).ToArray();
                        await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, calendar, calendar.Id);
                    }
                    else
                    {
                        logger.LogError("Could not remove user '{userEmail}' from calendar assignment: {resultItem2}",
                            user.Email, result.Item2);
                    }
                }

                // Delete MS user                
                await _graphUsersService.UserDelete(AuthenticationType.GraphService, user.MsAadId);

                // Flag user as deleted
                user.ActiveFlag = false;
                await _breezyCosmosService.UpsertItem(breezyContainers.Users, user, user.Email);

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}