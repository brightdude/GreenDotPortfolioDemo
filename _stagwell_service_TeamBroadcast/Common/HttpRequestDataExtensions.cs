using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    internal static class HttpRequestDataExtensions
    {
        public static T GetBodyObject<T>(this HttpRequestData requestData, out List<ValidationResult> validationResults)
        {
            T obj = default;
            validationResults = new List<ValidationResult>();

            if (requestData == null || requestData.Body == null || requestData.Body.Length == 0) return obj;
            using (var reader = new StreamReader(requestData.Body))
            {
                obj = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }

            var isValid = Validator.TryValidateObject(obj, new ValidationContext(obj), validationResults, true);
            if (!isValid) return default;
            return obj;
        }

        public static async Task<HttpResponseData> OkResponse(this HttpRequestData req, object responseData)
        {
            return await ResponseWithBody(req, responseData, HttpStatusCode.OK);
        }

        public static async Task<HttpResponseData> CreatedResponse(this HttpRequestData req, object responseData)
        {
            return await ResponseWithBody(req, responseData, HttpStatusCode.Created);
        }

        private static async Task<HttpResponseData> ResponseWithBody(this HttpRequestData req, object responseData, HttpStatusCode status)
        {
            var updatedResponse = req.CreateResponse(status);
            if (responseData is IEnumerable<object>)
            {
                var obj = Newtonsoft.Json.Linq.JArray.FromObject(responseData);
                await updatedResponse.WriteStringAsync(obj.ToString());
            }
            else
            {
                var obj = Newtonsoft.Json.Linq.JObject.FromObject(responseData);
                await updatedResponse.WriteStringAsync(obj.ToString());
            }
            return updatedResponse;
        }

        /// <summary>
        /// Returns a 400 response based on the provided validation results.
        /// </summary>
        public static async Task<HttpResponseData> UnauthorisedResponse(this HttpRequestData req, ILogger logger)
        {
            var msg = $"Unauthorised access to {req.FunctionContext.FunctionDefinition.Name}";
            logger.LogError(msg);
            var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await errorResponse.WriteStringAsync(msg);
            return errorResponse;
        }

        /// <summary>
        /// Returns a 400 response based on the provided validation results.
        /// </summary>
        public static Task<HttpResponseData> BadRequestResponse(this HttpRequestData req, ILogger logger, IEnumerable<ValidationResult> validationResults)
        {
            var errorMessage = validationResults.Any() ? string.Join('\n', validationResults.Select(r => r.ErrorMessage)) : "Bad Request";
            return BadRequestResponse(req, logger, errorMessage);
        }

        /// <summary>
        /// Returns a 400 response based on the provided reason.
        /// </summary>
        public static Task<HttpResponseData> BadRequestResponse(this HttpRequestData req, ILogger logger, string reason)
        {
            var ex = new Exception(reason);
            return BadRequestResponse(req, logger, ex);
        }

        /// <summary>
        /// Returns a 400 response based on the provided exception.
        /// </summary>
        public static async Task<HttpResponseData> BadRequestResponse(this HttpRequestData req, ILogger logger, Exception ex)
        {
            logger.LogError(ex, "Bad Request");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;
        }

        /// <summary>
        /// Returns a 404 response for a failed retrieve with the provided id.
        /// </summary>
        public static Task<HttpResponseData> NotFoundResponse(this HttpRequestData req, ILogger logger, string containerName, string id)
        {
            return NotFoundResponse(req, logger, $"Could not find document with id '{id}' in container '{containerName}'");
        }

        /// <summary>
        /// Returns a 404 response for a failed retrieve with the provided id.
        /// </summary>
        public static async Task<HttpResponseData> NotFoundResponse(this HttpRequestData req, ILogger logger, string message)
        {
            var ex = new Exception(message);
            logger.LogError(ex, "Not Found");
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;
        }

        /// <summary>
        /// Returns a 409 response for a failed insert with the provided id.
        /// </summary>
        public static async Task<HttpResponseData> ConflictResponse(this HttpRequestData req, ILogger logger, string containerName, string id)
        {
            var ex = new Exception($"Attempted to insert document into container '{containerName}' with id '{id}' which already exists");
            logger.LogError(ex, "Conflict");
            var errorResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;
        }

        /// <summary>
        /// Returns a 409 response for a failed insert with the provided message.
        /// </summary>
        public static async Task<HttpResponseData> ConflictResponse(this HttpRequestData req, ILogger logger, string message)
        {
            var ex = new Exception(message);
            logger.LogError(ex, "Conflict");
            var errorResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;
        }

        /// <summary>
        /// Returns a generic 500 response based on the provided exception.
        /// </summary>
        public static async Task<HttpResponseData> ExceptionResponse(this HttpRequestData req, ILogger logger, Exception ex)
        {
            if (ex is CosmosException cex)
            {
                logger.LogError(cex, "Cosmos Error");
                var cosmosErrorResponse = req.CreateResponse(cex.StatusCode);
                await cosmosErrorResponse.WriteStringAsync(cex.Message);
                return cosmosErrorResponse;
            }

            logger.LogError(ex, "Internal Server Error");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal Server Error");
            return errorResponse;
        }
    }
}
