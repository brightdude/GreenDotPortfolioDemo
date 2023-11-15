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
    public partial class HoldingCall
    {        
        [OpenApiOperation(operationId: "holdingcall-externalcalendarid-post", Summary = "/holdingCall/{externalCalendarId} - POST", Description = "Creates a new holding call for the specified calendar")]
        [OpenApiParameter(name: "externalCalendarId", Description = "The unique external identifier for the calendar", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(HoldingCallCreateParams), Required = true, Description = "The holding call to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HoldingCall), Summary = "Retrieved OK", Description = "Successfully created the holding call")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The external calendar id was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("waitingrooms.ReadWriteAll")]
        [Function("HoldingCall_Create")]
        public async Task<HttpResponseData> HoldingCallCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "holdingCall/{externalCalendarId}")] HttpRequestData req, FunctionContext executionContext, string externalCalendarId)
        {
            var functionName = nameof(HoldingCallCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of '{functionName}' function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<HoldingCallCreateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                // Check path params match request body
                if (externalCalendarId != input.ExternalCalendarId)
                {
                    return await req.BadRequestResponse(logger, $"Path parameter for external calendar id '{externalCalendarId}' does not match request body '{input.ExternalCalendarId}'");
                }

                // Check start and end dates - not utc
                var now = DateTime.UtcNow;
                if (input.StartTime < now) return await req.BadRequestResponse(logger, $"The meeting start time {input.StartTime} cannot be in the past");
                if (input.EndTime < input.StartTime) return await req.BadRequestResponse(logger, $"The meeting end time {input.EndTime} cannot be earlier than the start time {input.StartTime}");

                // Get calendar
                var calendar = await _breezyCosmosService.GetCalendar(externalCalendarId);
                if (calendar == null) return await req.NotFoundResponse(logger, $"Could not find a calendar with external calendar id of '{externalCalendarId}'");

                // Create online meeting                
                var meetingId = Guid.NewGuid();
                var meetingName = "Waiting Room call for " + calendar.CalendarName;
                var graphOnlineMeeting = await _graphOnlineMeetingsService.OnlineMeetingsCreateOrGet(AuthenticationType.WaitingRoomService, meetingId.ToString(), input.StartTime, input.EndTime, meetingName);

                // Create holding call object and insert into database
                var holdingCall = new HoldingCall()
                {
                    EndTime = input.EndTime,
                    IsExpired = false,
                    MSJoinInfo = MsJoinInfo.FromGraphObject(graphOnlineMeeting),
                    MsMeetingId = graphOnlineMeeting.Id,
                    MsThreadId = graphOnlineMeeting.ChatInfo.ThreadId,
                    StartTime = input.StartTime
                };

                foreach (var item in calendar.HoldingCalls.Where(a => !a.IsExpired.GetValueOrDefault(false)))
                {
                    try
                    {
                        await _graphOnlineMeetingsService.OnlineMeetingsDelete(AuthenticationType.WaitingRoomService, item.MsMeetingId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Could not delete online meeting with id '{MeetingId}' from Microsoft Graph. Reason: '{ErrorMessage}'", item.MsMeetingId, ex.Message);
                    }
                    logger.LogInformation("Expiring Holding Call '{MsMeetingId}' at '{StartTime}' for calendar '{ExternalCalendarId}'.", item.MsMeetingId, item.StartTime, input.ExternalCalendarId);
                    item.IsExpired = true;

                }
                calendar.HoldingCalls = calendar.HoldingCalls.Append(holdingCall).ToArray();

                await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, calendar, calendar.Id);

                logger.LogInformation("Function '{functionName}' succeeded!", functionName);
                return await req.CreatedResponse(holdingCall);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
