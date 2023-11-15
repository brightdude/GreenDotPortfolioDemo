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
    public partial class HoldingCall
    {
        [OpenApiOperation(operationId: "teams-teamid-holdingcall-members-get", Summary = "/teams/{teamId}/holdingCalls/members - GET", Description = "Retrieves a list of all holding calls members for a specific team")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MeetingConversationMembersResponse[]), Summary = "Retrieved OK", Description = "Successfully retrieved all the members")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The team id was not found")]
        [RequiredPermission("teams.waitingrooms.ReadAll", "teams.waitingrooms.ReadWriteAll")]
        [Function("HoldingCalls_Members_List")]
        public async Task<HttpResponseData> HoldingCallMembersList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/holdingCalls/members")] HttpRequestData req,
            FunctionContext executionContext,
            string teamId)
        {
            var functionName = nameof(HoldingCallMembersList);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);
            
            try
            {
                // Get facility for team
                var facility = await _breezyCosmosService.GetFacilityByTeam(teamId);
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, $"The facility with a team id '{teamId}' does not exist");
                }

                // Get all facility calendar
                var calendarQuery = new QueryDefinition("SELECT c.externalCalendarId, c.calendarName, c.holdingCalls FROM c WHERE c.facilityId = @facilityId")
                    .WithParameter("@facilityId", facility.Id);
                var calendar = await _breezyCosmosService.GetItem<CalendarHoldingCalls>(breezyContainers.Calendars, calendarQuery);

                if (calendar == null)
                {
                    logger.LogInformation("There are no calendars associated with a team id '{teamId}'", teamId);
                    return await req.OkResponse(Array.Empty<MeetingConversationMembersResponse>());
                }

                // Get service account which owns the online meeting
                var ownerEmail = _credentialOptions.CurrentValue.WaitingRoomService.Username;

                // Get all meetings members
                var meetingsMembers = new List<MeetingConversationMembersResponse>();

                foreach (var holdingCall in calendar.HoldingCalls.Where(x => !x.IsExpired.GetValueOrDefault()))
                {
                    var members = (await _graphChatsService.ChatMembersList(AuthenticationType.WaitingRoomService, holdingCall.MsThreadId)).OfType<Microsoft.Graph.AadUserConversationMember>();
                    meetingsMembers.Add(new MeetingConversationMembersResponse
                    {
                        ExternalCalendarId = calendar.ExternalCalendarId,
                        MsMeetingId = holdingCall.MsMeetingId,
                        Members = members.Where(m => m.Email != ownerEmail).Select(x => x.ToDto()).ToArray()
                    });
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(meetingsMembers);
            }
            catch (Exception ex)
            {
                logger.LogInformation("Function {functionName} failed!", functionName);
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
