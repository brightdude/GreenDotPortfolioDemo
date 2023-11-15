using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class Event
    {
        [OpenApiOperation(operationId: "event-delete", Summary = "/event/{externalId} - DELETE", Description = "Delete a specific event")]
        [OpenApiParameter(name: "externalId", Description = "The external id of an event to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted successfully", Description = "Successfully deleted the event")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The event was not found")]
        [RequiredPermission("scheduledmeetings.ReadWriteAll")]
        [Function("Event_Delete")]
        public async Task<HttpResponseData> EventDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "event/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(EventDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get the event for this external id
                var externalIdLower = externalId.ToLower();
                var eventList = await _breezyCosmosService.ListEventsByExternalId(externalIdLower, true);
                if (!eventList.Any())
                {
                    return await req.NotFoundResponse(logger, $"An event with external id '{externalIdLower}' was not found");
                }
                else if (eventList.Count() > 1)
                {
                    logger.LogError("There are {eventListCount} events with external id '{externalIdLower}' : {eventListIds}",
                        eventList.Count(), externalIdLower, eventList.Select(x => x.Id).ToJsonString());
                    return await req.ConflictResponse(logger, $"Cannot delete event because there are {eventList.Count()} events with external id '{externalIdLower}'");
                }
                var @event = eventList.First();


                // Delete online meeting
                try
                {                  
                    await _graphOnlineMeetingsService.OnlineMeetingsDelete(AuthenticationType.ScheduledEventService, @event.MsMeetingId);
                }
                catch (ServiceException ex)
                {
                    logger.LogError("Failed to delete online meeting '{MsMeetingId}' with status code {StatusCode}: {Exception}",
                        @event.MsMeetingId, ex.StatusCode, ex.Message);
                }

                // Flag event as deleted in database
                @event.Status = "Deleted";
                await _breezyCosmosService.UpsertItem(breezyContainers.Events, @event, @event.CalendarId);

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