using Ardalis.GuardClauses;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public class Token
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthorisationService _authService;

        public Token(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IAuthorisationService authService)
        {
            _configuration = Guard.Against.Null(configuration, nameof(configuration));
            _httpClientFactory = Guard.Against.Null(httpClientFactory, nameof(httpClientFactory));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        [OpenApiIgnore]
        [Function("Auth_GetAccessTokenForTeamsAppPermissions")]
        public async Task<HttpResponseData> GetAccessTokenForTeamsAppPermissions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "token/onBehalfOf")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var functionName = nameof(GetAccessTokenForTeamsAppPermissions);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);

            try
            {
                var input = req.GetBodyObject<OnBehalfOfParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                var secret = _configuration.GetValue<string>($"client-id-{input.ClientId}");
                if (secret == null)
                {
                    return await req.NotFoundResponse(logger, $"Unrecognised client id '{input.ClientId}'");
                }

                var ssoSecret = JsonConvert.DeserializeObject<SsoSecret>(secret);

                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://login.microsoftonline.com/{ssoSecret.Tenant}/oauth2/v2.0/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {"grant_type", ssoSecret.GrantType },
                        {"client_id", ssoSecret.ClientId },
                        {"client_secret", ssoSecret.ClientSecret },
                        {"assertion", input.SsoToken },
                        {"scope", ssoSecret.Scope },
                        {"requested_token_use", ssoSecret.RequestedTokenUse }
                    })
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("SSO login succeeded");                   
                   
                    var successResponse = req.CreateResponse(response.StatusCode);                   
                    successResponse.Body = response.Content.ReadAsStream();
                    return successResponse;
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogError("SSo login failed {statusCode} {content}", content, response.StatusCode);

                    var errorResponse = req.CreateResponse(response.StatusCode);
                    await errorResponse.WriteStringAsync(content);
                    return errorResponse;
                }
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        public class OnBehalfOfParams
        {
            [OpenApiProperty(Description = "The id of the audience in the SSO token")]
            [DataType(DataType.Text)]
            [JsonProperty("clientId")]
            [JsonRequired]
            public string ClientId { get; set; }

            [OpenApiProperty(Description = "The SSO token")]
            [DataType(DataType.Text)]
            [JsonProperty("ssoToken")]
            [JsonRequired]
            public string SsoToken { get; set; }
        }

        public class SsoSecret
        {
            [DataType(DataType.Text)]
            [JsonProperty("client_id")]
            public string ClientId { set; get; }

            [DataType(DataType.Text)]
            [JsonProperty("client_secret")]
            public string ClientSecret { set; get; }

            [DataType(DataType.Text)]
            [JsonProperty("grant_type")]
            public string GrantType { set; get; }

            [DataType(DataType.Text)]
            [JsonProperty("requested_token_use")]
            public string RequestedTokenUse { set; get; }

            [DataType(DataType.Text)]
            [JsonProperty("scope")]
            public string Scope { set; get; }

            [DataType(DataType.Text)]
            [JsonProperty("tenant")]
            public string Tenant { set; get; }
        }
    }
}
