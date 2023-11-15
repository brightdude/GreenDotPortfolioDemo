using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphServiceClientFactory
    {
        GraphServiceClient CreateClient(ApplicationCredential appCredential, BasicCredential userCredential = default);
    }

    internal class GraphServiceClientFactory : IGraphServiceClientFactory
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        public GraphServiceClientFactory(
            IMemoryCache memoryCache,
            IHttpClientFactory httpClientFactory,
            ILogger<GraphServiceClientFactory> logger)
        {
            _memoryCache = Guard.Against.Null(memoryCache, nameof(memoryCache));
            _httpClientFactory = Guard.Against.Null(httpClientFactory, nameof(httpClientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public GraphServiceClient CreateClient(ApplicationCredential appCredential, BasicCredential userCredential = default)
        {
            var authProvider = new DelegateAuthenticationProvider(async (request) =>
            {
                var token = await GetAccessToken(appCredential, userCredential);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            });

            return new GraphServiceClient(_httpClientFactory.CreateClient())
            {
                AuthenticationProvider = authProvider
            };
        }

        private async Task<MsToken> GetAccessToken(ApplicationCredential appCredential, BasicCredential userCredential = default)
        {
            userCredential ??= BasicCredential.None;

            using var _ = _logger.BeginScope(nameof(GetAccessToken));

            if (!_memoryCache.TryGetValue(userCredential, out var accessToken))
            {
                var httpClient = _httpClientFactory.CreateClient();

                var content = new Dictionary<string, string> {
                    {"grant_type", userCredential.IsNone ? "client_credentials" : "password" },
                    {"resource", appCredential.Audience },
                    {"client_id", appCredential.ClientId },
                    {"client_secret", appCredential.Secret }
                };

                if (!userCredential.IsNone)
                {
                    content.Add("username", userCredential.Username);
                    content.Add("password", userCredential.Password);
                }

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://login.microsoftonline.com/{appCredential.Tenant}/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(content)
                };

                var response = await PollyPolicies.WaitAndRetryHttpResponseMessageAsync(logger: _logger)
                    .ExecuteAsync(_ => httpClient.SendAsync(request), new Context(nameof(GetAccessToken)));

                if (response.IsSuccessStatusCode)
                {
                    var token = JsonConvert.DeserializeObject<MsToken>(await response.Content.ReadAsStringAsync());
                    _memoryCache.Set(userCredential, token, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(token.ExpiresIn)));
                    _logger.LogDebug("Retrieved token from server, {username}", userCredential.Username ?? "None");
                    return token;
                }
                else
                {
                    _logger.LogError("Failed, {reasonPhrase}, {response}", response.ReasonPhrase, await response.Content.ReadAsStringAsync());
                    throw new Exception(response.ReasonPhrase);
                }
            }
            else
            {
                _logger.LogDebug("Return cached token, {username}", userCredential.Username ?? "None");
                return accessToken as MsToken;
            }
        }

        private class MsToken
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public long ExpiresIn { get; set; }
        }
    }

    internal static class GraphServiceClientFactoryExtensions
    {
        public static GraphServiceClient GetClient(this IGraphServiceClientFactory factory, CredentialOptions options, AuthenticationType authType)
        {
            return authType switch
            {
                AuthenticationType.ScheduledEventService => factory.CreateClient(options.GraphService, options.ScheduledEventService),
                AuthenticationType.OnDemandMeetingService => factory.CreateClient(options.GraphService, options.OnDemandMeetingService),
                AuthenticationType.WaitingRoomService => factory.CreateClient(options.GraphService, options.WaitingRoomService),
                AuthenticationType.GraphService => factory.CreateClient(options.GraphService),
                _ => throw new ArgumentOutOfRangeException(nameof(authType)),
            };
        }
    }
}
