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
        [OpenApiOperation(operationId: "event-update", Summary = "/event/{externalId} - PATCH", Description = "Update an existing event")]
        [OpenApiParameter(name: "externalId", Description = "The external id of an event to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(EventCreateUpdateParams), Required = true, Description = "The event to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Event), Summary = "Updated", Description = "The event was updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The event does not exist")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("scheduledmeetings.ReadWriteAll")]
        [Function("Event_Update")]
        public async Task<HttpResponseData> EventUpdate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "event/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(EventUpdate);
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
                if (!eventList.Any())
                {
                    return await req.NotFoundResponse(logger, $"An event with external id '{externalIdLower}' was not found");
                }
                else if (eventList.Count() > 1)
                {
                    logger.LogError("There are {eventListCount} events with external id '{externalIdLower}' : {eventListIds}", 
                        eventList.Count(), externalIdLower, eventList.Select(x => x.Id).ToJsonString());
                    return await req.ConflictResponse(logger, $"Cannot update an event because there are {eventList.Count()} events with external id '{externalIdLower}'");
                }
                var existingEvent = eventList.First();

                // Check if calendar exists
                var calendar = await _breezyCosmosService.GetCalendar(input.ExternalCalendarId);
                if (calendar == null)
                {
                    return await req.BadRequestResponse(logger, $"The calendar with externalCalendarId '{input.ExternalCalendarId}' for this event '{externalIdLower}' does not exist.");
                }

                // Check if facility exists
                var facility = await _breezyCosmosService.GetFacility(calendar.FacilityId);
                if (facility == null)
                {
                    return await req.BadRequestResponse(logger, $"A facility with id '{calendar.FacilityId}' was not found.");
                }

                // Update the online meeting, but only if something has changed
                var subject = string.IsNullOrWhiteSpace(input.Subject) ? $"{input.CaseId} - {input.CaseTitle}" : input.Subject;
                MsTeamsMeetingInfo meetingInfo = null;
                if (existingEvent.Subject != subject || existingEvent.StartTime != input.StartTime || existingEvent.EndTime != input.EndTime)
                {
                    meetingInfo = await UpdateTeamsMeeting(existingEvent.MsMeetingId, subject, input.StartTime, input.EndTime);
                }
                else
                {
                    logger.LogInformation("Did not update meeting '{MsMeetingId}' because no information has changed", existingEvent.MsMeetingId);
                }

                // Create updated Event document and update database
                var updatedEvent = new Event
                {
                    Body = input.Body ?? existingEvent.Body,
                    CalendarId = calendar.Id,
                    CaseId = input.CaseId ?? existingEvent.CaseId,
                    CaseTitle = input.CaseTitle ?? existingEvent.CaseTitle,
                    CaseType = input.CaseType ?? existingEvent.CaseType,
                    EndTime = input.EndTime,
                    EventType = input.EventType ?? existingEvent.EventType,
                    ExternalCalendarId = input.ExternalCalendarId,
                    ExternalId = externalIdLower,
                    FacilityId = facility.Id,
                    Id = existingEvent.Id,
                    MsJoinInfo = meetingInfo == null ? existingEvent.MsJoinInfo : meetingInfo.MsJoinInfo,
                    MsMeetingId = meetingInfo == null ? existingEvent.MsMeetingId : meetingInfo.MsMeetingId,
                    MsThreadId = meetingInfo == null ? existingEvent.MsThreadId : meetingInfo.MsThreadId,
                    OptionalAttendees = input.OptionalAttendees ?? existingEvent.OptionalAttendees,
                    RequiredAttendees = input.RequiredAttendees ?? existingEvent.RequiredAttendees,
                    StartTime = input.StartTime,
                    Status = "Active",
                    Subject = subject,
                    MsTeamId = facility.Team.MsTeamId
                };
                await _breezyCosmosService.UpsertItem(breezyContainers.Events, updatedEvent, calendar.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(updatedEvent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        private async Task<MsTeamsMeetingInfo> UpdateTeamsMeeting(string msMeetingId, string meetingName, DateTime startTime, DateTime endTime)
        {            
            var graphOnlineMeeting = await _graphOnlineMeetingsService.OnlineMeetingsUpdate(AuthenticationType.ScheduledEventService, msMeetingId, startTime, endTime, meetingName);
            return new MsTeamsMeetingInfo
            {
                MsMeetingId = graphOnlineMeeting.Id,
                MsThreadId = graphOnlineMeeting.ChatInfo.ThreadId,
                MsJoinInfo = MsJoinInfo.FromGraphObject(graphOnlineMeeting)
            };
        }
    }
}