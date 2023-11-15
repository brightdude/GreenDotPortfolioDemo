using Breezy.Muticaster.Schema;
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
    public partial class Event
    {
        [OpenApiOperation(operationId: "teams-teamid-events-members-get", Summary = "/teams/{teamId}/events/members - GET", Description = "Retrieves a list of all events members for a specific team")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventConversationMembersResponse[]), Summary = "Retrieved OK", Description = "Successfully retrieved all the members")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The team id was not found")]
        [RequiredPermission("teams.scheduledmeetings.ReadAll", "teams.scheduledmeetings.ReadWriteAll")]
        [Function("Events_Members_List")]
        public async Task<HttpResponseData> EventsMembersList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/events/members")] HttpRequestData req,
            FunctionContext executionContext,
            string teamId)
        {
            var functionName = nameof(EventsMembersList);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get all calendars for this team
                var calendars = await _breezyCosmosService.ListCalendarsForTeam(teamId);
                if (!calendars.Any())
                {
                    logger.LogInformation("There are no calendars associated with a team id '{teamId}'", teamId);
                    return await req.OkResponse(Array.Empty<EventConversationMembersResponse>());
                }

                // Get all calendars events
                var eventsQuery = new QueryDefinition("SELECT * FROM c WHERE c.status = 'Active' AND ARRAY_CONTAINS(@calenderIds, c.calendarId) AND DateTimeAdd('hh', 1, c.endTime) > GetCurrentDateTime()")
                    .WithParameter("@calenderIds", calendars.Select(c => c.Id).ToArray());
                var events = await _breezyCosmosService.GetList<Event>(breezyContainers.Events, eventsQuery);

                if (events.IsEmpty())
                {
                    logger.LogInformation("There are no events associated with a team id '{teamId}'", teamId);
                    return await req.OkResponse(Array.Empty<EventConversationMembersResponse>());
                }

                // Get service account which owns the online meeting
                var ownerEmail = _credentialOptions.CurrentValue.ScheduledEventService.Username;

                // Get all events members
                var eventsMembers = new List<EventConversationMembersResponse>();

                foreach (var @event in events)
                {
                    var members = (await _graphChatsService.ChatMembersList(AuthenticationType.ScheduledEventService, @event.MsThreadId)).OfType<Microsoft.Graph.AadUserConversationMember>();
                    eventsMembers.Add(new EventConversationMembersResponse
                    {
                        EventId = @event.Id,
                        Members = members.Where(m => m.Email != ownerEmail).Select(x => x.ToDto()).ToArray()
                    });
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(eventsMembers);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
