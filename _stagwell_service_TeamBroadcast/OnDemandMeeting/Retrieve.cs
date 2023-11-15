using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FTR.VirtualJustice
{
    public partial class OnDemandMeeting
    {
        [OpenApiOperation(operationId: "teams-teamid-ondemandmeetings-get", Summary = "/teams/{teamId}/onDemandMeetings - GET", Description = "Gets a list of all active on-demand meetings within the next hour")]
        [OpenApiParameter(name: "teamId", Description = "The id of the team", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(OnDemandMeeting[]), Summary = "Retrieved OK", Description = "Successfully retrieved the list of on-demand meeetings")]
        [RequiredPermission("teams.ondemandmeetings.ReadAll")]
        [Function("OnDemandMeetings_List")]
        public async Task<HttpResponseData> OnDemandMeetingsList([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{teamId}/onDemandMeetings")] HttpRequestData req, FunctionContext executionContext, string teamId)
        {
            var functionName = nameof(OnDemandMeetingsList);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.msTeamId = @teamId AND DateTimeAdd('hh', 1, c.endDateTime) > GetCurrentDateTime() AND c.activeFlag = true")
                    .WithParameter("@teamId", teamId);
                var onDemandMeetings = await _courtConnectCosmosService.GetList<OnDemandMeeting>(CourtConnectContainers.OnDemandMeetings, queryDef);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.OkResponse(onDemandMeetings);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }
    }
}
