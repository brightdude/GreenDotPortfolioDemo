using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FTR.VirtualJustice
{
    public partial class OnDemandMeeting
    {
        [OpenApiOperation(operationId: "teams-teamid-ondemandmeetings-meetingid-delete", Summary = "/teams/{teamId}/onDemandMeetings/{meetingId} - DELETE", Description = "Deletes an on-demand meeting with the specified id")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "meetingId", Description = "The id of the meeting to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "application/json", bodyType: typeof(OnDemandMeeting), Summary = "Retrieved OK", Description = "Successfully deleted the on-demand meeting")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The on-demand meeting was not found")]
        [RequiredPermission("teams.ondemandmeetings.ReadWriteAll")]
        [Function("OnDemandMeetings_Delete")]
        public async Task<HttpResponseData> OnDemandMeetingsDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "teams/{teamId}/onDemandMeetings/{meetingId}")] HttpRequestData req, FunctionContext executionContext, string teamId, string meetingId)
        {
            var functionName = nameof(OnDemandMeetingsDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of '{functionName}' function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Check the meeting exists
                var meeting = await _courtConnectCosmosService.GetOnDemandMeeting(teamId, meetingId);
                if (meeting == null) return await req.NotFoundResponse(logger, $"An on-demand meeting with id '{meetingId}' was not found in team '{teamId}'");

                try
                {
                    // Delete the Teams online meeting              
                    await _graphOnlineMeetingsService.OnlineMeetingsDelete(AuthenticationType.OnDemandMeetingService, meeting.MsMeetingId);
                }
                catch (Exception ex)
                { 
                    logger.LogWarning("Could not delete online meeting with id '{MeetingId}' from Microsoft Graph. Reason: '{ErrorMessage}'", meetingId, ex.Message);
                }
                // Change on-demand meeting status to inactive
                meeting.ActiveFlag = false;
                await _courtConnectCosmosService.UpsertItem(CourtConnectContainers.OnDemandMeetings, meeting, teamId);

                logger.LogInformation("Function '{functionName}' succeeded!", functionName);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
