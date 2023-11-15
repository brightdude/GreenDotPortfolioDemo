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
    public partial class Event
    {
        [OpenApiOperation(operationId: "event-create", Summary = "/event/{externalId} - POST", Description = "Create a new event")]
        [OpenApiParameter(name: "externalId", Description = "The external id of an event to create", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(EventCreateUpdateParams), Required = true, Description = "The event to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Event), Summary = "Created", Description = "The event was created successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "An existing event record already exists")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("scheduledmeetings.ReadWriteAll")]
        [Function("Event_Create")]
        public async Task<HttpResponseData> EventCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "event/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(EventCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var externalIdLower = externalId.ToLower();

                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<EventCreateUpdateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }
                if (input.StartTime < DateTime.UtcNow)
                {
                    return await req.BadRequestResponse(logger, $"Events cannot be created for dates in the past. The event start {input.StartTime:u} is invalid.");
                }
                if (input.EndTime < input.StartTime)
                {
                    return await req.BadRequestResponse(logger, $"End time {input.EndTime:u} is before start time {input.StartTime:u}.");
                }

                // Check if event exists
                var eventList = await _breezyCosmosService.ListEventsByExternalId(externalIdLower, true);
                if (eventList.Any())
                {
                    return await req.ConflictResponse(logger, $"An event record with the externalId '{externalIdLower}' already exists.");
                }

                // Check if calendar exists
                var calendar = await _breezyCosmosService.GetCalendar(input.ExternalCalendarId);
                if (calendar == null)
                {
                    return await req.BadRequestResponse(logger, $"A calendar with externalCalendarId '{input.ExternalCalendarId}' was not found.");
                }

                // Check if facility exists
                var facility = await _breezyCosmosService.GetFacility(calendar.FacilityId);
                if (facility == null)
                {
                    return await req.BadRequestResponse(logger, $"A facility with id '{calendar.FacilityId}' was not found.");
                }

                // Create the online meeting
                var subject = string.IsNullOrWhiteSpace(input.Subject) ? $"{input.CaseId} - {input.CaseTitle}" : input.Subject;
                var eventId = Guid.NewGuid().ToString();
                var meetingInfo = await CreateTeamsMeeting(externalIdLower, subject, input.StartTime, input.EndTime, logger);

                // Create new Event document and insert into database
                var newEvent = new Event
                {
                    Body = input.Body,
                    CalendarId = calendar.Id,
                    CaseId = input.CaseId,
                    CaseTitle = input.CaseTitle,
                    CaseType = input.CaseType,
                    EndTime = input.EndTime,
                    EventType = input.EventType,
                    ExternalCalendarId = input.ExternalCalendarId,
                    ExternalId = externalIdLower,
                    FacilityId = facility.Id,
                    Id = eventId,
                    MsJoinInfo = meetingInfo.MsJoinInfo,
                    MsMeetingId = meetingInfo.MsMeetingId,
                    MsThreadId = meetingInfo.MsThreadId,
                    OptionalAttendees = input.OptionalAttendees,
                    RequiredAttendees = input.RequiredAttendees,
                    StartTime = input.StartTime,
                    Status = "Active",
                    Subject = subject,
                    MsTeamId = facility.Team.MsTeamId
                };
                await _breezyCosmosService.CreateItem(breezyContainers.Events, newEvent, calendar.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(newEvent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        private async Task<MsTeamsMeetingInfo> CreateTeamsMeeting(string externalId, string meetingName, DateTime startTime, DateTime endTime, ILogger logger)
        {          
            var graphOnlineMeeting = await _graphOnlineMeetingsService.OnlineMeetingsCreateOrGet(AuthenticationType.ScheduledEventService, externalId, startTime, endTime, meetingName);
            return new MsTeamsMeetingInfo
            {
                MsMeetingId = graphOnlineMeeting.Id,
                MsThreadId = graphOnlineMeeting.ChatInfo.ThreadId,
                MsJoinInfo = MsJoinInfo.FromGraphObject(graphOnlineMeeting)
            };
        }
    }
}