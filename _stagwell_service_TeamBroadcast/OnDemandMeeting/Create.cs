using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace FTR.VirtualJustice
{
    public partial class OnDemandMeeting
    {
        [OpenApiOperation(operationId: "teams-teamid-ondemandmeetings-post", Summary = "/teams/{teamId}/onDemandMeetings - POST", Description = "Creates a new on-demand meeting for the specified team")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(OnDemandMeetingCreateParams), Required = true, Description = "The on-demand meeting to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(OnDemandMeeting), Summary = "Retrieved OK", Description = "Successfully created the on-demand meeting")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The team id was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("teams.ondemandmeetings.ReadWriteAll")]
        [Function("OnDemandMeetings_Create")]
        public async Task<HttpResponseData> OnDemandMeetingsCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "teams/{teamId}/onDemandMeetings")] HttpRequestData req, FunctionContext executionContext, string teamId)
        {
            var functionName = nameof(OnDemandMeetingsCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<OnDemandMeetingCreateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                // Check path params match request body
                if (teamId != input.TeamId)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for team id '{teamId}' does not match request body '{input.TeamId}'");
                }

                // Check start and end dates
                var now = DateTime.UtcNow;
                var startTime = input.StartTime ?? now;
                if (startTime < now) return await req.BadRequestResponse(logger, $"The meeting start time {startTime} cannot be in the past");
                var endTime = input.EndTime ?? now.AddHours(1);
                if (endTime < startTime) return await req.BadRequestResponse(logger, $"The meeting end time {endTime} cannot be earlier than the start time {startTime}");

                // Get facility for this team
                var facility = await _courtConnectCosmosService.GetFacilityByTeam(teamId);
                if (facility == null) return await req.NotFoundResponse(logger, $"A facility associated with team id '{teamId}' was not found");

                // Create online meeting                
                var meetingId = Guid.NewGuid();
                var graphOnlineMeeting = await _graphOnlineMeetingsService.OnlineMeetingsCreateOrGet(AuthenticationType.OnDemandMeetingService, meetingId.ToString(), startTime, endTime, input.MeetingName);

                // Create on-demand meeting object and insert into database
                var onDemandMeeting = new OnDemandMeeting()
                {
                    ActiveFlag = true,
                    AudioConferencing = AudioConferencing.FromGraphObject(graphOnlineMeeting.AudioConferencing),
                    EndTime = endTime,
                    FacilityId = facility.Id,
                    Id = meetingId.ToString(),
                    JoinUrl = graphOnlineMeeting.JoinWebUrl,
                    MeetingName = input.MeetingName,
                    MsMeetingId = graphOnlineMeeting.Id,
                    MsTeamId = input.TeamId,
                    MsThreadId = graphOnlineMeeting.ChatInfo.ThreadId,
                    Organizer = input.Organizer,
                    StartTime = startTime
                };
                await _courtConnectCosmosService.CreateItem(CourtConnectContainers.OnDemandMeetings, onDemandMeeting, input.TeamId);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(onDemandMeeting);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
