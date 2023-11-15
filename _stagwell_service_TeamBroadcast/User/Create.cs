using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class User
    {
        [OpenApiOperation(operationId: "users-post", Summary = "/users - POST", Description = "Creates a new user")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UserCreateUpdateParams), Required = true, Description = "The user to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(User), Summary = "User created", Description = "The user was created successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The user already exists")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("users.ReadWriteAll")]
        [Function("User_Create")]
        public async Task<HttpResponseData> UserCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(UserCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<UserCreateUpdateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }
                validationResults = CheckRequiredValues(input, new string[] { "AccessLevel", "DepartmentId", "DisplayName", "FirstName", "FullName", "LastName", "RoleId" });
                if (validationResults.Any())
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                // Check user exists
                var email = input.Email.ToLower();
                var userId = Guid.NewGuid().ToString();
                var existingUser = await _breezyCosmosService.GetUser(email);
                if (existingUser != null)
                {
                    if (existingUser.ActiveFlag)
                    {
                        return await req.ConflictResponse(logger, $"A user with email address '{email}' already exists");
                    }
                    else
                    {
                        userId = existingUser.Id;
                    }
                }

                // Check department exists
                var department = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.Departments, input.DepartmentId, input.DepartmentId);
                if (department == null || department.Status != "Active")
                {
                    return await req.NotFoundResponse(logger, $"A department with id '{input.DepartmentId}' was not found");
                }

                // Check personnel role exists
                var role = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.PersonnelRoles, input.RoleId, input.RoleId);
                if (role == null || role.Status != "Active")
                {
                    return await req.NotFoundResponse(logger, $"A personnel role with id '{input.RoleId}' was not found");
                }

                // Check title exists
                LookupItem title = null;
                if (!string.IsNullOrWhiteSpace(input.TitleId))
                {
                    title = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.Titles, input.TitleId, input.TitleId);
                    if (title == null || title.Status != "Active")
                    {
                        return await req.NotFoundResponse(logger, $"A title with id '{input.TitleId}' was not found");
                    }
                }

                // Create new user object
                var newUser = new User
                {
                    AccessLevel = input.AccessLevel,
                    ActiveFlag = true,
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    DisplayName = input.DisplayName,
                    Email = email,
                    FirstName = input.FirstName,
                    Id = userId,
                    LastName = input.LastName,
                    Phone = input.Phone,
                    RoleId = role.Id,
                    RoleName = role.Name,
                    TitleId = title?.Id,
                    TitleName = title?.Name,
                    UsageLocation = (await _tenantSettingsService.GetTenantSettings()).DefaultUsageLocation
                };

                // Create new ms user via invitation
                var invitation = await MsUserCreate(newUser, logger);
                newUser.MsAadId = invitation.InvitedUser.Id;
                newUser.InviteRedeemUrl = invitation.InviteRedeemUrl;

                // Create user in database
                var createResponse = await _breezyCosmosService.CreateItem(breezyContainers.Users, newUser, email);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(createResponse.Resource);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Checks for null or empty values in the provided user parameters object.
        /// </summary>
        private static List<ValidationResult> CheckRequiredValues(UserCreateUpdateParams obj, IEnumerable<string> requiredProperties)
        {
            var result = new List<ValidationResult>();
            var properties = obj.GetType().GetProperties();
            foreach (var propName in requiredProperties)
            {
                var prop = properties.Where(p => p.Name == propName).FirstOrDefault();
                if (prop != null)
                {
                    if (string.IsNullOrWhiteSpace(prop.GetValue(obj) as string))
                        result.Add(new ValidationResult($"The required property '{propName}' was not provided"));
                }
            }
            return result;
        }
    }
}