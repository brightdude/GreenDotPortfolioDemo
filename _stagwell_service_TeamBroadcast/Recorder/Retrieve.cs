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
    public partial class Recorder
    {
        [OpenApiOperation(operationId: "recorders-list-all", Summary = "/recorders - GET", Description = "Gets a list of all recorders")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Recorder[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of recorders")]
        [RequiredPermission("recorders.ReadAll", "recorders.ReadWriteAll")]
        [Function("Recorder_ListAll")]
        public async Task<HttpResponseData> RecorderListAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recorders")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(RecorderListAll);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var recorders = await _breezyCosmosService.ListRecorders();

                // Append the related calendar ids to each recorder
                if (recorders.Any())
                {
                    var calendarIds = await GetCalendarIds(recorders.Select(r => r.Email));
                    foreach (var recorder in recorders)
                    {
                        recorder.Calendars = calendarIds.ContainsKey(recorder.Email) ? calendarIds[recorder.Email].ToArray() : Array.Empty<string>();
                    }
                }

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JArray.FromObject(recorders.ToArray());
                await retrievedResponse.WriteStringAsync(obj.ToString());
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        private class QueryOutput
        {
            public string CalendarId { get; set; }
            public string Recorder { get; set; }
        }

        private async Task<IDictionary<string, List<string>>> GetCalendarIds(IEnumerable<string> emails)
        {
            var dict = new Dictionary<string, List<string>>();

            var queryDef = new QueryDefinition("SELECT c.id as CalendarId, r as Recorder FROM Calendars c JOIN r IN c.recorders where ARRAY_CONTAINS(@emails, r)")
                .WithParameter("@emails", emails.ToArray());
            foreach (var item in await _breezyCosmosService.GetList<QueryOutput>(breezyContainers.Calendars, queryDef))
            {
                if (!dict.ContainsKey(item.Recorder)) dict.Add(item.Recorder, new List<string>());
                if (!dict[item.Recorder].Contains(item.CalendarId)) dict[item.Recorder].Add(item.CalendarId);
            }

            return dict;
        }

        [OpenApiOperation(operationId: "recorders-get", Summary = "/recorders/{id} - GET", Description = "Gets a specific recorder")]
        [OpenApiParameter(name: "id", Description = "The id of the recorder to retrieve", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Recorder), Summary = "Retrieved OK", Description = "Successfully retrieved the recorder")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The recorder id does not exist")]
        [RequiredPermission("recorders.ReadAll", "recorders.ReadWriteAll")]
        [Function("Recorder_Retrieve")]
        public async Task<HttpResponseData> RecorderRetrieve([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recorders/{id}")] HttpRequestData req, FunctionContext executionContext, string id)
        {
            var functionName = nameof(RecorderRetrieve);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Get the recorder
                var recorder = await _breezyCosmosService.GetItem<Recorder>(breezyContainers.Recorders, id, id);
                if (recorder == null)
                {
                    return await req.NotFoundResponse(logger, $"The recorder with id '{id}' does not exist");
                }

                // Append the related calendar ids
                var calendarIds = await GetCalendarIds(new string[] { recorder.Email });
                recorder.Calendars = calendarIds.ContainsKey(recorder.Email) ? calendarIds[recorder.Email].ToArray() : Array.Empty<string>();

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JObject.FromObject(recorder);
                await retrievedResponse.WriteStringAsync(obj.ToString());
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
