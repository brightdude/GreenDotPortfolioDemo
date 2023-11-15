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
        [OpenApiOperation(operationId: "holdingcall-list", Summary = "/holdingCall - GET", Description = "Gets a list of all holding calls")]
        [OpenApiParameter(name: "includeExpired", Description = "Indicates whether expired Holding Calls are included in the list", In = ParameterLocation.Query, Required = false, Type = typeof(bool))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CalendarHoldingCall[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of holding calls")]
        [RequiredPermission("waitingrooms.ReadAll", "waitingrooms.ReadWriteAll")]
        [Function("HoldingCall_RetrieveAll")]
        public async Task<HttpResponseData> HoldingCallRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "holdingCall")] HttpRequestData req, FunctionContext executionContext, bool? includeExpired)
        {
            var functionName = nameof(HoldingCallRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var sql = "SELECT c.externalCalendarId, c.calendarName, hc as holdingCall FROM c JOIN hc in c.holdingCalls";
                if (!(includeExpired ?? false)) sql += " WHERE hc.isExpired = false";
                var calendarHoldingCalls = await _breezyCosmosService.GetList<CalendarHoldingCall>(breezyContainers.Calendars, new QueryDefinition(sql));

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(calendarHoldingCalls);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        [OpenApiOperation(operationId: "holdingcall-externalcalendarid-list", Summary = "/holdingCall/{externalCalendarId} - GET", Description = "Gets a list of Holding Calls for a Calendar")]
        [OpenApiParameter(name: "externalCalendarId", Description = "The id of the Calendar we're retrieving Holding Calls for", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "includeExpired", Description = "Indicates whether expired Holding Calls are included in the list", In = ParameterLocation.Query, Required = false, Type = typeof(bool))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CalendarHoldingCalls[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of holding calls")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The external calendar id does not exist")]
        [RequiredPermission("waitingrooms.ReadAll", "waitingrooms.ReadWriteAll")]
        [Function("HoldingCall_RetrieveByCalendar")]
        public async Task<HttpResponseData> HoldingCallRetrieveByCalendar([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "holdingCall/{externalCalendarId}")] HttpRequestData req, FunctionContext executionContext, string externalCalendarId, bool? includeExpired)
        {
            var functionName = nameof(HoldingCallRetrieveByCalendar);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Retrieve the calendar
                var queryDef = new QueryDefinition("SELECT c.externalCalendarId, c.calendarName, c.holdingCalls FROM c WHERE c.externalCalendarId = @externalCalendarId")
                    .WithParameter("@externalCalendarId", externalCalendarId);
                var calendar = await _breezyCosmosService.GetItem<CalendarHoldingCalls>(breezyContainers.Calendars, queryDef);
                if (calendar == null)
                {
                    return await req.NotFoundResponse(logger, $"Could not find a calendar with id '{externalCalendarId}'");
                }

                // Filter expired holding calls
                if (!(includeExpired ?? false))
                {
                    calendar.HoldingCalls = calendar.HoldingCalls.Where(hc => !(hc.IsExpired ?? false)).ToArray();
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(new CalendarHoldingCalls[1] { calendar });
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        [OpenApiOperation(operationId: "holdingcall-teamid-list", Summary = "/teams/{teamId}/holdingCalls - GET", Description = "Gets a list of Holding Calls for a Team")]
        [OpenApiParameter(name: "teamId", Description = "The id of the Team we're retrieving Holding Calls for", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "includeExpired", Description = "Indicates whether expired Holding Calls are included in the list", In = ParameterLocation.Query, Required = false, Type = typeof(bool))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CalendarHoldingCalls[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of holding calls")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The facility or calendar does not exist")]
        [RequiredPermission("teams.waitingrooms.ReadAll", "waitingrooms.ReadWriteAll")]
        [Function("HoldingCall_RetrieveByTeamId")]
        public async Task<HttpResponseData> HoldingCallRetrieveByTeamId([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/holdingCalls")] HttpRequestData req, FunctionContext executionContext, string teamId, bool? includeExpired)
        {
            var functionName = nameof(HoldingCallRetrieveByTeamId);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Retrieve the facility
                var facility = await _breezyCosmosService.GetFacilityByTeam(teamId);
                if (facility == null)
                {
                    return await req.NotFoundResponse(logger, $"A facility with team id '{teamId}' was not found");
                }

                // Retrieve the calendar
                var queryDef = new QueryDefinition("SELECT c.externalCalendarId, c.calendarName, c.holdingCalls FROM c WHERE c.facilityId = @facilityId")
                    .WithParameter("@facilityId", facility.Id);                
                var calendars = await _breezyCosmosService.GetList<CalendarHoldingCalls>(breezyContainers.Calendars, queryDef);

                var calendarHoldingCalls = new List<CalendarHoldingCalls>();

                foreach (var calendar in calendars)
                {

                    // Filter expired holding calls
                    if (!(includeExpired ?? false))
                    {
                        calendar.HoldingCalls = calendar.HoldingCalls.Where(hc => !(hc.IsExpired ?? false)).ToArray();
                    }
                    calendarHoldingCalls.Add(calendar);
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(calendarHoldingCalls);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
