using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IAuthorisationService
    {
        Task<bool> CheckAuthorisation(HttpRequestData req, ILogger logger);
    }

    internal class AuthorisationService : IAuthorisationService
    {
        public async Task<bool> CheckAuthorisation(HttpRequestData req, ILogger logger)
        {
            // Look for a required permission attribute on the calling function
            RequiredPermissionAttribute attribute = null;
            var stackTrace = new StackTrace();
            for (var i = 0; i < 20; i++) // we need to go up the stack an unknown number of times, but not too many
            {
                var frame = stackTrace.GetFrame(i);
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    if (method.CustomAttributes.Count() > 1 && method.Name != nameof(CheckAuthorisation))
                    {
                        attribute = method.GetCustomAttribute<RequiredPermissionAttribute>();
                        if (attribute != null) break;
                    }
                }
            }

            // If there is no required permissions we don't have to check anything
            var acceptedPermissions = attribute?.AcceptedPermissions;
            if (acceptedPermissions == null || !acceptedPermissions.Any())
            {
                logger.LogInformation("Accepted permissions not defined, request authorised");
                return true;
            }

            // Check the access token
            if (!req.Headers.TryGetValues("Authorization", out var values))
            {
                logger.LogError("No authorization header, request unauthorised");
                return false;
            }
            var elements = (values.FirstOrDefault() ?? "").Split(' ');
            if (elements.Length < 2 && elements[0] != "Bearer")
            {
                logger.LogError("No bearer token, request unauthorised");
                return false;
            }
            var accessToken = elements[1];
            var claimsPrincipal = await ValidateAccessToken(accessToken, logger);
            if (claimsPrincipal == null)
            {
                logger.LogError("Access token could not be validated, request unauthorised");
                return false;
            }

            // Extract the roles/scopes from the access token claims
            var providedPermissions = new List<string>();
            var scopes = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope");
            if (scopes != null) providedPermissions.AddRange(scopes.Value.Split(' '));
            // Remove 'api.' prefix from role names so they match scope names, to simplify matching later
            var roles = claimsPrincipal.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(r => r.Value.Replace("api.", ""));
            if (roles != null) providedPermissions.AddRange(roles);
            logger.LogInformation($"Accepted scopes for function: {string.Join(", ", acceptedPermissions)}");
            logger.LogInformation($"Provided scopes from claims principal: {string.Join(", ", providedPermissions)}");

            // See if we have a permission matching any of the roles/scopes
            var matches = acceptedPermissions.Intersect(providedPermissions);
            if (matches.Any())
            {
                logger.LogInformation($"Found match on {string.Join(", ", matches)}, request authorised");
                return true;
            }
            else
            {
                logger.LogError($"No matching permissions, request unauthorised. Required permissions are {string.Join(", ", acceptedPermissions)}");
                return false;
            }
        }

        private static string _tenantId;
        private static string TenantId
        {
            get
            {
                if (_tenantId == null)
                {
                    var pathToAssembly = Assembly.GetExecutingAssembly().Location;
                    var basePath = System.IO.Path.GetDirectoryName(pathToAssembly);
                    var config = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                    var graphServiceCreds = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationCredential>(config.GetValue<string>("graph-service-creds"));
                    _tenantId = graphServiceCreds?.Tenant;
                }
                return _tenantId;
            }
        }

        private static string _apimProxyAppClientId;
        private static string ApimProxyAppClientId
        {
            get
            {
                if (_apimProxyAppClientId == null)
                {
                    var pathToAssembly = Assembly.GetExecutingAssembly().Location;
                    var basePath = System.IO.Path.GetDirectoryName(pathToAssembly);
                    var config = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                    _apimProxyAppClientId = config.GetValue<string>("apim-proxy-app-client-id");
                }
                return _apimProxyAppClientId;
            }
        }

        private static async Task<ClaimsPrincipal> ValidateAccessToken(string accessToken, ILogger logger)
        {
            var configManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<OpenIdConnectConfiguration>(
                $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
            var config = await configManager.GetConfigurationAsync();

            // Initialize the token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                // App Id URI and AppId of this service application are both valid audiences.
                ValidAudiences = new string[] { $"api://{ApimProxyAppClientId}", ApimProxyAppClientId },
                ValidIssuers = new string[] { $"https://sts.windows.net/{TenantId}/" },
                IssuerSigningKeys = config.SigningKeys
            };

            try
            {
                var tokenValidator = new JwtSecurityTokenHandler();
                return tokenValidator.ValidateToken(accessToken, validationParameters, out var securityToken);
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                logger.LogError($"Expected audience api://{ApimProxyAppClientId}, got audience {ex.InvalidAudience}");
            }
            return null;
        }
    }
}
