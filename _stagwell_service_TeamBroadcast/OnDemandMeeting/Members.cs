using FTR.VirtualJustice.Schema;
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

namespace FTR.VirtualJustice
{
    public partial class OnDemandMeeting
    {
        [OpenApiOperation(operationId: "teams-teamid-ondemandmeetings-members-get", Summary = "/teams/{teamId}/onDemandMeetings/members - GET", Description = "Retrieves a list of all on-demand meetings members for a specific team")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventConversationMembersResponse[]), Summary = "Retrieved OK", Description = "Successfully retrieved all the members")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The team id was not found")]
        [RequiredPermission("teams.ondemandmeetings.ReadAll", "teams.ondemandmeetings.ReadWriteAll")]
        [Function("OnDemandMeetings_Members_List")]
        public async Task<HttpResponseData> OnDemandMeetingsMembersList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/onDemandMeetings/members")] HttpRequestData req,
            FunctionContext executionContext,
            string teamId)
        {
            var functionName = nameof(OnDemandMeetingsMembersList);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get facility for this team
                var facility = await _courtConnectCosmosService.GetFacilityByTeam(teamId);
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, $"The facility with a team id '{teamId}' does not exist");
                }

                // Get all on-demand meetings
                var onDemandMeetingsQuery = new QueryDefinition("SELECT * FROM c WHERE c.activeFlag = true AND c.msTeamId = @teamId AND DateTimeAdd('hh', 1, c.endDateTime) > GetCurrentDateTime()")
                    .WithParameter("@teamId", teamId);

                var onDemandMeetings = await _courtConnectCosmosService.GetList<OnDemandMeeting>(CourtConnectContainers.OnDemandMeetings, onDemandMeetingsQuery);

                var eventsMembers = new List<EventConversationMembersResponse>();
                if (!onDemandMeetings.IsEmpty())
                {
                    // Get service account which owns the online meeting
                    var ownerEmail = _credentialOptions.CurrentValue.OnDemandMeetingService.Username;

                    // Get all meetings members
                    foreach (var onDemandMeeting in onDemandMeetings.Where(x => !x.MsThreadId.IsEmpty()))
                    {
                        var members = (await _graphChatsService.ChatMembersList(AuthenticationType.OnDemandMeetingService, onDemandMeeting.MsThreadId)).OfType<Microsoft.Graph.AadUserConversationMember>();
                        eventsMembers.Add(new EventConversationMembersResponse
                        {
                            EventId = onDemandMeeting.Id,
                            Members = members.Where(m => m.Email != ownerEmail).Select(x => x.ToDto()).ToArray()
                        });
                    }
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(eventsMembers);
            }
            catch (Exception ex)
            {
                logger.LogInformation("Function {functionName} failed!", functionName);
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
