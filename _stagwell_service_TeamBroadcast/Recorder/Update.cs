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
using Breezy.Muticaster.Schema;
using System.ComponentModel.DataAnnotations;


namespace Breezy.Muticaster
{
    public partial class Recorder
    {
        [OpenApiOperation(operationId: "recorders-id-patch", Summary = "/recorders/{id} - PATCH", Description = "Updates an existing recorder")]
        [OpenApiParameter(name: "id", Description = "The id of the recorder to update", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateRecorderRequest), Required = true, Description = "The recorder to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Recorder), Summary = "Updated", Description = "The recorder was updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The recorder location already exists")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "Department, recording type, or stream type was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("recorders.ReadWriteAll")]
        [Function("Recorder_Update")]
        public async Task<HttpResponseData> RecorderUpdate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "recorders/{id}")] HttpRequestData req,
            FunctionContext executionContext,
            string id)
        {
            var functionName = nameof(RecorderUpdate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var input = req.GetBodyObject<UpdateRecorderRequest>(out List<ValidationResult> validationResults);
                if (!validationResults.IsEmpty())
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                var provisioningStatusList = await _breezyCosmosService.ListRecorderProvisioningStatus();
                if (!input.ProvisioningStatus.IsEmpty() && !provisioningStatusList.Contains(input.ProvisioningStatus))
                {
                    return await req.BadRequestResponse(logger, $"Provisioning status '{input.ProvisioningStatus}' is not an allowed value");
                }

                var department = await _breezyCosmosService.GetDepartment(input.DepartmentId);
                if (department == null)
                {
                    return await req.NotFoundResponse(logger, $"The department with id '{input.DepartmentId}' does not exist");
                }

                var recordingType = await _breezyCosmosService.GetRecordingType(input.RecordingTypeId);
                if (recordingType == null)
                {
                    return await req.NotFoundResponse(logger, $"The recording type with id '{input.RecordingTypeId}' does not exist");
                }

                var streamType = await _breezyCosmosService.GetStreamingType(input.StreamTypeId);
                if (streamType == null)
                {
                    return await req.NotFoundResponse(logger, $"The stream type with id '{input.StreamTypeId}' does not exist");
                }

                var recorder = await _breezyCosmosService.GetRecorderById(id);
                if (recorder == null)
                {
                    return await req.ConflictResponse(logger, $"The recorder with id '{id}' does not exist");
                }

                if (!input.LocationName.IsEmpty() && recorder.LocationName != input.LocationName)
                {
                    var find = await _breezyCosmosService.GetRecorderByLocation(input.LocationName);
                    if (find != null && find.ActiveFlag)
                    {
                        return await req.ConflictResponse(logger, $"The recorder with location '{input.LocationName}' already exists");
                    }
                }               

                var user = await _graphUsersService.UserUpdate(AuthenticationType.GraphService, recorder.MsAadId, new Microsoft.Graph.User
                {
                    Department = recorder.DepartmentName,
                    DisplayName = recorder.DisplayName,
                    MailNickname = recorder.LocationName
                });

                recorder.DepartmentId = department.Id;
                recorder.DepartmentName = department.Name;
                recorder.DisplayName = input.DisplayName ?? recorder.DisplayName;
                recorder.LocationName = input.LocationName ?? recorder.LocationName;
                recorder.ProvisioningStatus = input.ProvisioningStatus ?? recorder.ProvisioningStatus;
                recorder.RecordingTypeId = recordingType.Id;
                recorder.RecordingTypeName = recordingType.Name;
                recorder.StreamTypeId = streamType.Id;
                recorder.StreamTypeName = streamType.Name;

                var upsertResponse = await _breezyCosmosService.UpsertItem(breezyContainers.Recorders, recorder, recorder.Id);

                var response = req.CreateResponse(HttpStatusCode.OK);
                var responseData = Newtonsoft.Json.Linq.JObject.FromObject(upsertResponse.Resource);
                responseData.Property("activeFlag").Remove();
                await response.WriteAsJsonAsync(responseData);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return response;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

    }
}
