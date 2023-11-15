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
        [OpenApiOperation(operationId: "teams-teamid-ondemandmeetings-meetingid-messages-post", Summary = "/teams/{teamId}/onDemandMeetings/{meetingId}/messages - POST", Description = "Add a chat message to the on-demand meeting")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph team id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "meetingId", Description = "The id of the meeting to add a chat message to", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ChatMessageCreateParams), Required = true, Description = "The on-demand meeting chat message to add")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(ChatMessage), Summary = "Retrieved OK", Description = "Successfully created the on-demand meeting chat message")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The team id was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("teams.ondemandmeetings.ReadWriteAll")]
        [Function("OnDemandMeetings_Messages_Create")]
        public async Task<HttpResponseData> OnDemandMeetingsMessagesCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "teams/{teamId}/onDemandMeetings/{meetingId}/messages")] HttpRequestData req, FunctionContext executionContext, string teamId, string meetingId)
        {
            var functionName = nameof(OnDemandMeetingsMessagesCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<ChatMessageCreateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }
                if (string.IsNullOrWhiteSpace(input.Content))
                {
                    return await req.BadRequestResponse(logger, "Chat message cannot be blank");
                }

                // Check path params match request body
                if (teamId != input.TeamId)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for team id '{teamId}' does not match request body '{input.TeamId}'");
                }
                if (meetingId != input.MeetingId)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for meeting id '{meetingId}' does not match request body '{input.MeetingId}'");
                }

                // Check the meeting exists
                var meeting = await _courtConnectCosmosService.GetOnDemandMeeting(teamId, meetingId);
                if (meeting == null) return await req.NotFoundResponse(logger, $"Could not find an on-demand meeting with id '{meetingId}' in team '{teamId}'");

                // Create chat message               
                var chatMessage = await _graphChatsService.ChatMessagesCreate(AuthenticationType.OnDemandMeetingService, meeting.MsThreadId, input.Content);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(ChatMessage.FromGraphObject(chatMessage));
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
