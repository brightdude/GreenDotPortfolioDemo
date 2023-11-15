using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;


namespace Breezy.Muticaster
{
    public partial class Recorder
    {
        [OpenApiOperation(operationId: "recorders-id-delete", Summary = "/recorders/{id} - DELETE", Description = "Delete a specific recorder")]
        [OpenApiParameter(name: "id", Description = "The id of the recorder to delete", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Deleted successfully", Description = "Successfully deleted the recorder")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The recorder was not found")]
        [RequiredPermission("recorders.ReadWriteAll")]
        [Function("Recorder_Delete")]
        public async Task<HttpResponseData> RecorderDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "recorders/{id}")] HttpRequestData req,
            FunctionContext executionContext,
            string id)
        {
            var functionName = nameof(RecorderDelete);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var recorder = await _breezyCosmosService.GetRecorderById(id);
                if (recorder == null || !recorder.ActiveFlag)
                {
                    return await req.NotFoundResponse(logger, $"The recorder with ID '{id}' does not exist");
                }

                var calendarsQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.recorders, @recorderEmail)")
                    .WithParameter("@recorderEmail", recorder.Email);

                var calendars = await _breezyCosmosService.GetList<Calendar>(breezyContainers.Calendars, calendarsQuery);               

                foreach (var calendar in calendars)
                {
                    var recorders = calendar.Recorders.Where(x => !x.Equals(recorder.Email)).ToArray();
                    if (calendar.Recorders.Length > recorders.Length)
                    {
                        logger.LogInformation("Remove reorder {RecorderEmail} from {ExternalCalendarId}", recorder.Email, calendar.ExternalCalendarId);
                        calendar.Recorders = recorders;
                        await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, calendar, calendar.Id);
                    }
                }

                recorder.ActiveFlag = false;
                await _breezyCosmosService.UpsertItem(breezyContainers.Recorders, recorder, recorder.Id);
               
                await _graphUsersService.UserDelete(AuthenticationType.GraphService,recorder.MsAadId);

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
