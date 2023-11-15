using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class HoldingCall
    {
        [OpenApiOperation(operationId: "holdingcall-externalcalendarid-delete", Summary = "/holdingCall/{externalCalendarId} - DELETE", Description = "Deletes active holding call from the specified calendar")]
        [OpenApiParameter(name: "externalCalendarId", Description = "The unique external identifier for the calendar", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Delete Successful", Description = "Successfully removed active holding calls")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The external calendar id was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("waitingrooms.ReadWriteAll")]
        [Function("HoldingCall_Delete")]
        public async Task<HttpResponseData> HoldingCallDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "holdingCall/{externalCalendarId}")] HttpRequestData req, FunctionContext executionContext, string externalCalendarId)
        {
            var functionName = nameof(HoldingCallDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of '{functionName}' function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get calendar
                var calendar = await _breezyCosmosService.GetCalendar(externalCalendarId);
                if (calendar == null) return await req.NotFoundResponse(logger, $"Could not find a calendar with external calendar id of '{externalCalendarId}'");

                // Expire any active holding call for the calendar                
                var itemsChanged = false;
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
                    logger.LogInformation("Expiring Holding Call '{MsMeetingId}' at '{StartTime}' for calendar '{ExternalCalendarId}'.", item.MsMeetingId, item.StartTime, externalCalendarId);
                    item.IsExpired = true;
                    itemsChanged = true;
                }

                if (itemsChanged)
                {
                    await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, calendar, calendar.Id);
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
