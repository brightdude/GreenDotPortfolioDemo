using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class Calendar
    {
        [OpenApiOperation(operationId: "calendars-externalid-delete", Summary = "/calendars/{externalId} - DELETE", Description = "Deletes a calendar with the specified external id")]
        [OpenApiParameter(name: "externalId", Description = "The id of the calendar to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted OK", Description = "Successfully deleted the calendar")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The calendar was not found")]
        [RequiredPermission("calendars.ReadWriteAll")]
        [Function("Calendar_Delete")]
        public async Task<HttpResponseData> CalendarDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "calendars/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(CalendarDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get the calendar
                var calendar = await _breezyCosmosService.GetCalendar(externalId);
                if (calendar == null)
                {
                    return await req.NotFoundResponse(logger, $"The calendar with external id '{externalId.ToLower()}' does not exist");
                }

                // Remove each focus user from the team
                if (calendar.focusUsers != null && calendar.focusUsers.Any())
                {
                    logger.LogInformation("Removing {focusUsersLength} focus users from team associated with calendar {ExternalCalendarId}...",
                        calendar.focusUsers.Length, calendar.ExternalCalendarId);
                    var deleteCount = 0;
                    var errors = new List<string>();
                    foreach (var focusUserEmail in calendar.focusUsers)
                    {
                        var response = await UserRemoveCalendarAssignment(logger, focusUserEmail, calendar);
                        if (response.Item1 == 204)
                            deleteCount++;
                        else if (response.Item1 != 404)
                            errors.Add(response.Item2);
                    }
                    if (deleteCount > 0) logger.LogInformation("Successfully removed {deleteCount} users.", deleteCount);
                    if (errors.Any()) logger.LogError("Error(s) occurred while removing users from team, {errors}", errors.ToJsonString());
                }


                // Delete any non-expired holding calls
                var holdingCalls = calendar.HoldingCalls?.Where(hc => hc.IsExpired != true);
                if (holdingCalls.Any())
                {
                    logger.LogInformation("Deleting {holdingCallsCount} holding calls...", holdingCalls.Count());
                    var response = await _graphOnlineMeetingsService.OnlineMeetingsDeleteBulk(AuthenticationType.WaitingRoomService, holdingCalls.Select(hc => hc.MsMeetingId));
                    var successCount = response.Count(r => r.Value);
                    if (successCount > 0) logger.LogInformation("Successfully deleted {successCount} holding calls.", successCount);
                    var failedItems = response.Where(r => !r.Value).Select(r => r.Key);
                    if (failedItems.Any()) logger.LogError("Could not delete holding calls with the following meeting ids, {failedItems}", failedItems.ToJsonString());
                }

                // Get events associated with this calendar
                var events = await _breezyCosmosService.ListEventsByCalendar(calendar.Id);

                // Delete online meetings associated with events
                if (events.Any())
                {
                    logger.LogInformation("Deleting {eventsCount} events associated with calendar {calendar.ExternalCalendarId}...", events.Count(), ExternalCalendarId);
                    var response = await _graphOnlineMeetingsService.OnlineMeetingsDeleteBulk(AuthenticationType.ScheduledEventService, events.Select(e => e.MsMeetingId));

                    var successCount = 0;
                    foreach (var meetingId in response.Keys)
                    {
                        if (response[meetingId])
                        {
                            // Meeting was successfully deleted, flag event as deleted
                            var eventObj = events.First(e => e.MsMeetingId == meetingId);
                            eventObj.Status = "Deleted";
                            await _breezyCosmosService.UpsertItem(breezyContainers.Events, eventObj, calendar.Id);
                            successCount++;
                        }
                        else
                        {
                            logger.LogError("Could not delete online meeting with id {meetingId}", meetingId);
                        }
                    }
                    logger.LogInformation("Successfully deleted {successCount} events.", successCount);
                }

                // Delete the calendar document
                await _breezyCosmosService.DeleteItem<Calendar>(breezyContainers.Calendars, calendar.Id, calendar.Id);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Removes a focus user from all team channels
        /// </summary>
        public async Task<Tuple<int, string>> UserRemoveCalendarAssignment(ILogger logger, string email, Calendar calendar)
        {
            // Get facility
            var facility = await _breezyCosmosService.GetFacility(calendar.FacilityId);
            return await UserRemoveCalendarAssignment(logger, email, facility, calendar.Id);
        }

        /// <summary>
        /// Removes a focus user from all team channels
        /// </summary>
        private async Task<Tuple<int, string>> UserRemoveCalendarAssignment(ILogger logger, string email, Facility facility, string calendarId)
        {
            // Get user
            var user = await _breezyCosmosService.GetUser(email);
            if (user == null) return Tuple.Create(404, $"User with email '{email}' could not be found");

            // Find other calendars with same facility id and user
            var queryDef = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.focusUsers, @email) AND c.id != @calendarId AND c.facilityId = @facilityId")
                .WithParameter("@email", email)
                .WithParameter("@calendarId", calendarId)
                .WithParameter("@facilityId", facility.Id);
            var calendars = await _breezyCosmosService.GetList<Calendar>(breezyContainers.Calendars, queryDef);

            // Terminate if any calendars found - user needs to remain in team channels
            if (calendars.Any()) return new Tuple<int, string>(204, "Not Removed");

            // Remove user from all private channels associated with this facility
            var deleteCount = 0;

            foreach (var channel in facility.Team.Channels)
            {
                // Retrieve the channel info
                Microsoft.Graph.Channel msChannel;
                try
                {
                    msChannel = await _graphTeamsChannelsService.TeamChannelGet(AuthenticationType.GraphService, facility.Team.MsTeamId, channel.MsChannelId);
                }
                catch (Microsoft.Graph.ServiceException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.LogWarning("Could not find channel '{channelId}' in team '{teamId}'", channel.MsChannelId, facility.Team.MsTeamId);
                        continue;
                    }
                    else throw;
                }

                if (msChannel.MembershipType == Microsoft.Graph.ChannelMembershipType.Private)
                {
                    try
                    {
                        var memberList = await _graphTeamsChannelsMembersService.TeamChannelMemberList(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, channel.MsChannelId);
                        foreach (var member in memberList)
                        {
                            if (member.Id == user.MsAadId)
                            {
                                await _graphTeamsChannelsMembersService.TeamChannelMemberDelete(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, channel.MsChannelId, member.Id);
                                deleteCount++;
                            }
                        }
                    }
                    catch (Microsoft.Graph.ServiceException ex)
                    {
                        logger.LogError(ex, "Could not remove user '{MsAadId}' from channel '{channelId}' in team '{teamId}'",
                            user.MsAadId, channel.MsChannelId, facility.Team.MsTeamId);
                    }
                }
            }

            logger.LogInformation("Removed user '{email}' from {deleteCount} private channels", email, deleteCount);

            // Remove user from team
            var teamMembers = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId, user.MsAadId);
            if (teamMembers.Any())
            {
                await _graphTeamsMembersService.TeamMemberDelete(AuthenticationType.GraphService, facility.Team.MsTeamId, teamMembers.First().Id);
                logger.LogInformation("Removed user '{email}' from team '{teamId}'", email, facility.Team.MsTeamId);
            }

            return new Tuple<int, string>(204, "Removed");
        }
    }
}