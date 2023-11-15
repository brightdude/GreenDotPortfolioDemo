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
    public partial class Calendar
    {
        [OpenApiOperation(operationId: "calendars-get", Summary = "/calendars - GET", Description = "Retrieves all the calendars currently provisioned in the system")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CalendarSummary[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of calendars")]
        [RequiredPermission("calendars.ReadAll", "calendars.ReadWriteAll")]
        [Function("Calendar_RetrieveAll")]
        public async Task<HttpResponseData> CalendarRetrieveAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendars")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(CalendarRetrieveAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get the calendar list
                var queryDef = new QueryDefinition(@"
                    SELECT
                        c.id, c.externalCalendarId, c.calendarName, c.facilityId,
                        ARRAY_LENGTH(c.focusUsers) as personnelCount,
                        ARRAY_LENGTH(c.recorders) as recorderCount
                    FROM calendars c");
                var calendars = await _breezyCosmosService.GetList<CalendarSummary>(breezyContainers.Calendars, queryDef);

                // Get the list of associated facilities and populate facility name
                var facilities = await GetFacilityNames(calendars.Select(c => c.FacilityId).Distinct());
                foreach (var calendar in calendars)
                {
                    if (facilities.ContainsKey(calendar.FacilityId)) calendar.FacilityName = facilities[calendar.FacilityId];
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(calendars);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        private async Task<IDictionary<string, string>> GetFacilityNames(IEnumerable<string> idList)
        {
            var dict = new Dictionary<string, string>();
            if (!idList.Any()) return dict;

            var queryDef = new QueryDefinition($"SELECT c.id AS Id, c.displayName AS DisplayName FROM c WHERE c.id IN ({string.Join(',', idList.Select(id => "'" + id + "'"))})");
            foreach (var summaryItem in await _breezyCosmosService.GetList<FacilitySummary>(breezyContainers.Facilities, queryDef))
            {
                dict.Add(summaryItem.Id, summaryItem.DisplayName);
            }
            return dict;
        }

        private class FacilitySummary
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
        }

        [OpenApiOperation(operationId: "calendars-externalid-get", Summary = "/calendars/{externalId} - GET", Description = "Retrieves a calendar with the specified external id")]
        [OpenApiParameter(name: "externalId", Description = "The id of the calendar to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Calendar), Summary = "Retrieved OK", Description = "Successfully retrieved the calendar")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The calendar was not found")]
        [RequiredPermission("calendars.ReadAll", "calendars.ReadWriteAll")]
        [Function("Calendar_Retrieve")]
        public async Task<HttpResponseData> CalendarRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendars/{externalId}")] HttpRequestData req, FunctionContext executionContext, string externalId)
        {
            var functionName = nameof(CalendarRetrieve);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var calendar = await _breezyCosmosService.GetCalendar(externalId);
                if (calendar == null)
                {
                    return await req.NotFoundResponse(logger, $"The calendar with external id '{externalId.ToLower()}' does not exist");
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(calendar);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
