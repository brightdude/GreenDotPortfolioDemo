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
        [OpenApiOperation(operationId: "calendars-post", Summary = "/calendars - POST", Description = "Creates a new calendar")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CalendarCreateUpdateParams), Required = true, Description = "The calendar to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Calendar), Summary = "Calendar created", Description = "The calendar was created successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The calendar already exists")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The linked facility or department was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("calendars.ReadWriteAll")]
        [Function("Calendar_Create")]
        public async Task<HttpResponseData> CalendarCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calendars")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(CalendarCreate);
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

                // Check for existing calendar
                var externalCalendarId = input.ExternalCalendarId.ToLower();
                var existingCalendar = await _breezyCosmosService.GetCalendar(externalCalendarId);
                if (existingCalendar != null)
                {
                    return await req.ConflictResponse(logger, $"A calendar with external id '{externalCalendarId}' already exists");
                }

                // Check facility exists
                var facility = await _breezyCosmosService.GetFacility(input.FacilityId.ToLower());
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, "A facility with id '{FacilityId}' was not found", input.FacilityId);
                }

                // Check department exists
                var department = await _breezyCosmosService.GetItem<LookupItem>(breezyContainers.Departments, input.DepartmentId.ToLower(), input.DepartmentId.ToLower());
                if (department == null || department.Status != "Active")
                {
                    return await req.NotFoundResponse(logger, "A department with id '{DepartmentId}' was not found", input.DepartmentId);
                }

                // Check the linked recorder users exist                
                var recorders = new List<Recorder>();
                foreach (var recorderEmail in input.Recorders == null ? Array.Empty<string>() : input.Recorders.Select(r => r.ToLower()))
                {
                    var recorder = await _breezyCosmosService.GetRecorder(recorderEmail);
                    if (recorder == null || !recorder.ActiveFlag)
                    {
                        return await req.NotFoundResponse(logger, "An active recorder with email '{recorderEmail}' was not found", recorderEmail);
                    }
                    else
                    {
                        var msUser = await _graphUsersService.UserGet(AuthenticationType.GraphService, recorder.MsAadId);
                        if (msUser == null) return await req.NotFoundResponse(logger, "A graph user with email '{recorderEmail}' was not found", recorderEmail);
                        recorders.Add(recorder);
                    }
                }

                // Check the linked focus users exist
                var focusUserEmails = input.focusUsers == null ? Array.Empty<string>() : input.focusUsers.Select(u => u.ToLower()).ToArray();
                var focusUsers = Enumerable.Empty<User>();
                if (focusUserEmails.Any())
                {
                    focusUsers = await _breezyCosmosService.ListUsers(focusUserEmails, true);
                    if (focusUsers.Count() != input.focusUsers.Length)
                    {
                        var missingUsers = focusUserEmails.Except(focusUsers.Select(u => u.Email.ToLower()));
                        return await req.BadRequestResponse(logger, $"Could not add the following focus users which were not found in the system: {string.Join(", ", missingUsers)}");
                    }
                }

                // Create the calendar object
                var calendar = new Calendar()
                {
                    CalendarName = input.CalendarName,
                    focusUsers = focusUsers.Select(u => u.Email).ToArray(),
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    ExternalCalendarId = externalCalendarId,
                    FacilityId = facility.Id,
                    HoldingCalls = Array.Empty<HoldingCall>(),
                    Id = Guid.NewGuid().ToString(),
                    Recorders = recorders.Select(r => r.Email).ToArray(),
                    MsTeamId = facility.Team.MsTeamId
                };

                // Add recorder users to appropriate team and channels
                if (recorders.Any()) await _recorderService.RecorderAssignToTeamAndChannels(logger, recorders, facility);

                // Add focus users to appropriate team and channels
                if (focusUsers.Any()) await _userService.UserAssignToTeamAndChannels(logger, focusUsers, facility);

                // Insert object into database
                calendar = await _breezyCosmosService.CreateItem(breezyContainers.Calendars, calendar, calendar.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(calendar);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
