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
    public partial class Calendar
    {
        [OpenApiOperation(operationId: "calendars-externalid-patch", Summary = "/calendars/{externalId} - PATCH", Description = "Updates an existing calendar")]
        [OpenApiParameter(name: "externalId", Description = "The id of the calendar to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CalendarCreateUpdateParams), Required = true, Description = "The calendar to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(User), Summary = "Updated", Description = "The calendar was updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The calendar does not exist")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("calendars.ReadWriteAll")]
        [Function("Calendar_Update")]
        public async Task<HttpResponseData> CalendarUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "calendars/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(CalendarUpdate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<CalendarCreateUpdateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }
                var externalCalendarId = externalId.ToLower();
                if (input.ExternalCalendarId.ToLower() != externalCalendarId)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for external calendar id '{externalId}' does not match request body '{input.ExternalCalendarId}'");
                }

                // Get calendar
                var originalCalendar = await _breezyCosmosService.GetCalendar(externalCalendarId);
                if (originalCalendar == null)
                {
                    return await req.NotFoundResponse(logger, $"A calendar with external id '{externalCalendarId}' was not found");
                }

                // Check department exists
                LookupItem department = null;
                if (!string.IsNullOrWhiteSpace(input.DepartmentId))
                {
                    department = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.Departments, input.DepartmentId, input.DepartmentId);
                    if (department == null || department.Status != "Active")
                    {
                        return await req.NotFoundResponse(logger, $"A department with id '{input.DepartmentId}' was not found");
                    }
                }

                // Check facility exists
                var facility = await _breezyCosmosService.GetFacility(input.FacilityId ?? originalCalendar.FacilityId);
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, $"A facility with id '{input.FacilityId}' was not found");
                }

                // Check recorders exist and are active
                var updateRecorderEmails = input.Recorders == null ? Array.Empty<string>() : input.Recorders.Select(r => r.ToLower()).ToArray();
                var updateRecorders = new List<Recorder>();
                if (input.Recorders != null)
                {
                    foreach (var recorderEmail in input.Recorders)
                    {
                        var recorder = await _breezyCosmosService.GetRecorder(recorderEmail);
                        if (recorder == null || !recorder.ActiveFlag)
                        {
                            return await req.NotFoundResponse(logger, $"An active recorder with email '{recorderEmail}' was not found");
                        }
                        else
                        {
                            updateRecorders.Add(recorder);
                        }
                    }
                }

                // Check focus users exist and are active
                var updatefocusUsers = Enumerable.Empty<User>();
                var updatefocusUserEmails = Array.Empty<string>();
                if (input.focusUsers != null && input.focusUsers.Any())
                {
                    updatefocusUserEmails = input.focusUsers.Select(u => u.ToLower()).ToArray();
                    updatefocusUsers = await _breezyCosmosService.ListUsers(updatefocusUserEmails, true);
                    if (updatefocusUsers.Count() != updatefocusUserEmails.Length)
                    {
                        var missingUsers = updatefocusUserEmails.Except(updatefocusUsers.Select(u => u.Email.ToLower()));
                        return await req.BadRequestResponse(logger, $"Could not add the following focus users which were not found in the system: {string.Join(", ", missingUsers)}");
                    }
                }

                // If recorders have been defined, add or remove from the team and channels as required
                if (input.Recorders != null)
                {
                    var existingRecorderEmails = originalCalendar.Recorders.Select(r => r.ToLower());

                    // Find recorders which have been added and assign to team and channels
                    var addedRecorders = updateRecorders.Where(r => !existingRecorderEmails.Contains(r.Email));
                    if (addedRecorders.Any())
                    {
                        await _recorderService.RecorderAssignToTeamAndChannels(logger, addedRecorders, facility);
                    }

                    // Find recorders which have been removed and un-assign from team and channels
                    var removedRecorders = existingRecorderEmails.Where(email => !updateRecorderEmails.Contains(email));
                    if (removedRecorders.Any())
                    {
                        await _recorderService.RecorderUnassignFromTeamAndChannels(logger, removedRecorders, facility, originalCalendar.Id);
                    }
                }

                // If focus users have been defined, add or remove from the team and channels as required
                if (input.focusUsers != null)
                {
                    var existingfocusUserEmails = originalCalendar.focusUsers.Select(u => u.ToLower());

                    // Find recorders which have been added and assign to team and channels
                    var addedfocusUsers = updatefocusUsers.Where(u => !existingfocusUserEmails.Contains(u.Email));
                    if (addedfocusUsers.Any())
                    {
                        await _userService.UserAssignToTeamAndChannels(logger, addedfocusUsers, facility);
                    }

                    // Find recorders which have been removed and un-assign from team and channels
                    var removedfocusUsers = existingfocusUserEmails.Where(email => !updatefocusUserEmails.Contains(email));
                    if (removedfocusUsers.Any())
                    {
                        await _userService.UserUnassignFromTeamAndChannels(logger, removedfocusUsers, facility, originalCalendar.Id);
                    }
                }

                // If facility has changed move non-changed users and Calendar Events to new facility
                if (input.FacilityId != null && input.FacilityId != originalCalendar.FacilityId)
                {
                    // Users
                    var unchangedfocusUsers = originalCalendar.focusUsers.Intersect(input.focusUsers ?? Enumerable.Empty<string>());
                    if (unchangedfocusUsers.Any())
                    {
                        await _userService.UserUnassignFromTeamAndChannels(logger, unchangedfocusUsers, originalCalendar.FacilityId, originalCalendar.Id);
                        await _userService.UserAssignToTeamAndChannels(logger, unchangedfocusUsers, facility);
                    }
                    //Events
                    var events = await _breezyCosmosService.ListEventsByCalendar(originalCalendar.Id);
                    if (events.Any())
                    {
                        foreach (var e in events)
                        {
                            e.FacilityId = input.FacilityId;
                            await _breezyCosmosService.UpsertItem(breezyContainers.Events, e, originalCalendar.Id);
                        }
                    }
                }

                // Create the calendar object
                var updatedCalendar = new Calendar()
                {
                    CalendarName = input.CalendarName ?? originalCalendar.CalendarName,
                    focusUsers = input.focusUsers == null ? originalCalendar.focusUsers : updatefocusUserEmails,
                    DepartmentId = department == null ? originalCalendar.DepartmentId : department.Id,
                    DepartmentName = department == null ? originalCalendar.DepartmentName : department.Name,
                    ExternalCalendarId = externalCalendarId,
                    FacilityId = facility.Id,
                    HoldingCalls = (input.HoldingCalls ?? originalCalendar.HoldingCalls) ?? Array.Empty<HoldingCall>(),
                    Id = originalCalendar.Id,
                    Recorders = input.Recorders == null ? originalCalendar.Recorders : updateRecorderEmails,
                    MsTeamId = facility.Team.MsTeamId
                };

                // Update calendar
                var response = await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, updatedCalendar, updatedCalendar.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(response.Resource);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}