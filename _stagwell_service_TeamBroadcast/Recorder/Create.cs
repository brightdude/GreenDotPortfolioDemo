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
        [OpenApiOperation(operationId: "recorders-post", Summary = "/recorders - POST", Description = "Creates a new recorder")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateRecorderRequest), Required = true, Description = "The recorder to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Recorder), Summary = "Recorder created", Description = "Successfully created the recorder")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The recorder already exists")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "Department, recording type, or stream type was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Bad Request", Description = "Missing or invalid parameters")]
        [RequiredPermission("recorders.ReadWriteAll")]
        [Function("Recorder_Create")]
        public async Task<HttpResponseData> RecorderCreate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recorders")] HttpRequestData req, 
            FunctionContext executionContext)
        {
            var functionName = nameof(RecorderCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var input = req.GetBodyObject<CreateRecorderRequest>(out List<ValidationResult> validationResults);
                if (!validationResults.IsEmpty())
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
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

                var recorder = await _breezyCosmosService.GetRecorderByLocation(input.LocationName);
                if (recorder != null && recorder.ActiveFlag)
                {
                    return await req.ConflictResponse(logger, $"The recorder with location '{input.LocationName}' already exists");
                }
                
                var domains = await _graphDomainsService.DomainsList(AuthenticationType.GraphService);
                var defaultDomain = domains.First(x => x.IsDefault == true);
                var email = string.Concat(input.LocationName, '@', defaultDomain.Id).ToLowerInvariant();

                var user = await _graphUsersService.UserGet(AuthenticationType.GraphService, email);
                if (user == null)
                {                    
                    user = await _graphUsersService.UserCreate(AuthenticationType.GraphService, new Microsoft.Graph.User
                    {
                        AccountEnabled = true,
                        UserPrincipalName = email,
                        Department = department.Name,
                        DisplayName = input.DisplayName,
                        JobTitle = "Automated focus Recorder",
                        Mail = email,
                        MailNickname = input.LocationName,
                        PasswordPolicies = "DisablePasswordExpiration",
                        PasswordProfile = new Microsoft.Graph.PasswordProfile
                        {
                            ForceChangePasswordNextSignIn = false,
                            Password = _options.CurrentValue.DefaultRecorderPassword
                        }
                    });
                    logger.LogInformation("User created {UserPrincipalName}", user.UserPrincipalName);
                }

                recorder = new Recorder
                {
                    Id = recorder != null && !recorder.ActiveFlag ? recorder.Id : Guid.NewGuid().ToString(),
                    AccessLevel = "tier2",
                    ActiveFlag = true,
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    DisplayName = input.DisplayName,
                    Email = email,
                    LocationName = input.LocationName,
                    MsAadId = user.Id,
                    ProvisioningStatus = "Provisioning",
                    RecordingTypeId = recordingType.Id,
                    RecordingTypeName = recordingType.Name,
                    StreamTypeId = streamType.Id,
                    StreamTypeName = streamType.Name
                };

                var upsertResponse = await _breezyCosmosService.UpsertItem(breezyContainers.Recorders, recorder, recorder.Id);

                var response = req.CreateResponse(HttpStatusCode.Created);
                var responseData = Newtonsoft.Json.Linq.JObject.FromObject(upsertResponse.Resource);
                responseData.Property("activeFlag").Remove();
                await response.WriteAsJsonAsync(responseData, HttpStatusCode.Created); //Need to set status code, otherwise will be set to 200

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
