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
        [OpenApiOperation(operationId: "users-emailaddress-patch", Summary = "/users/{emailAddress} - PATCH", Description = "Updates an existing user")]
        [OpenApiParameter(name: "emailAddress", Description = "The user's email address", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UserCreateUpdateParams), Required = true, Description = "The user to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(User), Summary = "Updated", Description = "The user was updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The user does not exist")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("users.ReadWriteAll")]
        [Function("User_Update")]
        public async Task<HttpResponseData> UserUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "users/{emailAddress}")] HttpRequestData req, FunctionContext executionContext, string emailAddress)
        {
            var functionName = nameof(UserUpdate);
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
                var email = emailAddress.ToLower();
                if (input.Email.ToLower() != email)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for user email address '{email}' does not match request body '{input.Email}'");
                }

                // Get user
                var originalUser = await _breezyCosmosService.GetUser(email);
                if (originalUser == null)
                {
                    return await req.NotFoundResponse(logger, $"A user with email address '{email}' was not found");
                }

                // Check department exists
                LookupItem dept = null;
                if (!string.IsNullOrWhiteSpace(input.DepartmentId))
                {
                    dept = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.Departments, input.DepartmentId, input.DepartmentId);
                    if (dept == null || dept.Status != "Active")
                    {
                        return await req.NotFoundResponse(logger, $"A department with id '{input.DepartmentId}' was not found");
                    }
                }

                // Check personnel role exists
                LookupItem role = null;
                if (!string.IsNullOrWhiteSpace(input.RoleId))
                {
                    role = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.PersonnelRoles, input.RoleId, input.RoleId);
                    if (role == null || role.Status != "Active")
                    {
                        return await req.NotFoundResponse(logger, $"A personnel role with id '{input.RoleId}' was not found");
                    }
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

                // Update user properties
                var updateUser = new User
                {
                    AccessLevel = input.AccessLevel ?? originalUser.AccessLevel,
                    ActiveFlag = true,
                    DepartmentId = dept == null ? originalUser.DepartmentId : dept.Id,
                    DepartmentName = dept == null ? originalUser.DepartmentName : dept.Name,
                    DisplayName = input.DisplayName ?? originalUser.DisplayName,
                    Email = email,
                    FirstName = input.FirstName ?? originalUser.FirstName,
                    Id = originalUser.Id,
                    InviteRedeemUrl = input.InviteRedeemUrl ?? originalUser.InviteRedeemUrl,
                    LastName = input.LastName ?? originalUser.LastName,
                    MsAadId = originalUser.MsAadId,
                    Phone = input.Phone ?? originalUser.Phone,
                    RoleId = role == null ? originalUser.RoleId : role.Id,
                    RoleName = role == null ? originalUser.RoleName : role.Name,
                    TitleId = title?.Id,
                    TitleName = title?.Name,
                    UsageLocation = originalUser.UsageLocation
                };

                // Check ms user exists                
                var msUser = string.IsNullOrWhiteSpace(originalUser.MsAadId) ? null : await _graphUsersService.UserGet(AuthenticationType.GraphService, originalUser.MsAadId);
                if (msUser == null)
                {
                    // User does not exist, create one now
                    logger.LogInformation("MS user does not exist, creating...");
                    var invitation = await MsUserCreate(updateUser, logger);
                    logger.LogInformation("MS user created.");
                    updateUser.MsAadId = invitation.InvitedUser.Id;
                    updateUser.InviteRedeemUrl = invitation.InviteRedeemUrl;
                }
                else if (originalUser.DepartmentName != updateUser.DepartmentName || originalUser.DisplayName != updateUser.DisplayName || originalUser.FirstName != updateUser.FirstName || originalUser.LastName != updateUser.LastName)
                {
                    // User exists and needs to be updated
                    logger.LogInformation("Name field(s) have changed, updating MS user...");
                    await _graphUsersService.UserUpdate(AuthenticationType.GraphService, updateUser.MsAadId, new Microsoft.Graph.User
                    {
                        Department = updateUser.DepartmentName,
                        DisplayName = updateUser.DisplayName,
                        GivenName = updateUser.FirstName,
                        Surname = updateUser.LastName
                    });
                    logger.LogInformation("MS user updated.");
                }

                // Update user in database
                var response = await _breezyCosmosService.UpsertItem(breezyContainers.Users, updateUser, email);

                // Add or remove user from team channels if access level has changed
                if (originalUser.AccessLevel != updateUser.AccessLevel)
                {
                    await UpdateAccessLevel(updateUser, logger);
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(response.Resource);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Adds or removes a user from team channels based on their access level
        /// </summary>
        private async Task UpdateAccessLevel(User user,ILogger logger)
        {
            logger.LogInformation("Access level has changed, checking channel membership...");
            var changeCount = 0;

            var settingsChannels = (await _tenantSettingsService.GetTenantSettings()).Channels;

            // Get a list of facilities linked to this user
            var calendars = await _breezyCosmosService.ListCalendarsWithfocusUser(user.Email);
            var facilities = await _breezyCosmosService.ListFacilities(calendars.Select(c => c.FacilityId));

            // Loop through the channels in each facility
            foreach (var facility in facilities)
            {
                foreach (var facilityChannel in facility.Team.Channels)
                {
                    var settingsChannel = settingsChannels.FirstOrDefault(c => c.Name == facilityChannel.Name);
                    if (settingsChannel != null)
                    {
                        // If channel is private we might need to alter membership
                        if (settingsChannel.MembershipType == "private")
                        {
                            // Get the ms channel member corresponding to the current user
                            var channelMembers = await _graphTeamsChannelsMembersService.TeamChannelMemberList(AuthenticationType.GraphService, facility.Team.MsTeamId, facilityChannel.MsChannelId);
                            var member = channelMembers.FirstOrDefault(m => (m as Microsoft.Graph.AadUserConversationMember).UserId == user.MsAadId);

                            if (settingsChannel.AccessLevels.Contains(user.AccessLevel))
                            {
                                // User's access level matches this channel, so they should be a member
                                if (member == null)
                                {
                                    logger.LogInformation("User '{userEmail}' now has access to private channel '{ChannelName}', adding membership...",
                                        user.Email, facilityChannel.Name);
                                    await _graphTeamsChannelsMembersService.TeamChannelMemberCreate(AuthenticationType.GraphService, facility.Team.MsTeamId, facilityChannel.MsChannelId, user.MsAadId);
                                    logger.LogInformation("Membership updated.");
                                    changeCount++;
                                }
                            }
                            else
                            {
                                // User's access level does not match this channel, so they should not be a member
                                if (member != null)
                                {
                                    logger.LogInformation("User '{userEmail}' no longer has access to private channel '{ChannelName}', removing membership...",
                                        user.Email, facilityChannel.Name);
                                    await _graphTeamsChannelsMembersService.TeamChannelMemberDelete(AuthenticationType.GraphService, facility.Team.MsTeamId, facilityChannel.MsChannelId, member.Id);
                                    logger.LogInformation("Membership updated.");
                                    changeCount++;
                                }
                            }
                        }
                    }
                }
            }

            logger.LogInformation("Channel membership updated, {changeCount} changes were made.", changeCount);
        }
    }
}