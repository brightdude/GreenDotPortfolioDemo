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
        [OpenApiOperation(operationId: "event-list", Summary = "/event - GET", Description = "Retrieves a list of all non-expired active events")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Event[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of active events")]
        [RequiredPermission("scheduledmeetings.ReadAll", "scheduledmeetings.ReadWriteAll")]
        [Function("Event_RetrieveAll")]
        public async Task<HttpResponseData> EventRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "event")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(EventRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.status = 'Active' AND  DateTimeAdd('hh', 1, c.endTime) > GetCurrentDateTime()");
                var list = (await _breezyCosmosService.GetList<Event>(breezyContainers.Events, queryDef)).ToArray();

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse( list);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        [OpenApiOperation(operationId: "teams-teamid-events-get", Summary = "/teams/{teamId}/events - GET", Description = "Retrieves a list of all non-expired active events for a specific team")]
        [OpenApiParameter(name: "teamId", Description = "The Microsoft Graph teams id", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Event[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of active events")]
        [RequiredPermission("teams.scheduledmeetings.ReadAll", "teams.scheduledmeetings.ReadWriteAll")]
        [Function("Event_RetrieveAllForTeam")]
        public async Task<HttpResponseData> EventRetrieveAllForTeam([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/events")] HttpRequestData req, FunctionContext executionContext, string teamId)
        {
            var functionName = nameof(EventRetrieveAllForTeam);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            var list = new List<Event>();

            try
            {
                // Get calendars for this team
                var calendars = await _breezyCosmosService.ListCalendarsForTeam(teamId);
                if (calendars.Any())
                {
                    // Get active events with matching calendar id(s)
                    var csv = string.Join(',', calendars.Select(c => $"'{c.Id}'"));
                    var queryDef = new QueryDefinition($"SELECT * FROM c WHERE ARRAY_CONTAINS([{csv}], c.calendarId) AND DateTimeAdd('hh', 1, c.endTime) > GetCurrentDateTime() AND c.status = 'Active'");
                    list.AddRange(await _breezyCosmosService.GetList<Event>(breezyContainers.Events, queryDef));
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                return await req.OkResponse(list);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        [OpenApiOperation(operationId: "event-get", Summary = "/event/{externalId} - GET", Description = "Return details of a specific event")]
        [OpenApiParameter(name: "externalId", Description = "The external id of an event to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Event), Summary = "Retrieved OK", Description = "Successfully retrieved the event")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The event was not found")]
        [RequiredPermission("scheduledmeetings.ReadAll", "scheduledmeetings.ReadWriteAll")]
        [Function("Event_Retrieve")]
        public async Task<HttpResponseData> EventRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "event/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(EventRetrieve);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var externalIdLower = externalId.ToLower();
                var eventList = await _breezyCosmosService.ListEventsByExternalId(externalIdLower, true);
                if (!eventList.Any())
                {
                    return await req.NotFoundResponse(logger, $"An event with external id '{externalIdLower}' was not found");
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(eventList.First());
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}