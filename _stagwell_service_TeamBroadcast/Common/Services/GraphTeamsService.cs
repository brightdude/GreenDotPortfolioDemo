using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphTeamsService
    {
        Task<string> Create(AuthenticationType authType, string displayName, string description, string visibility);

        Task<TeamsAppInstallation> InstallApp(AuthenticationType authType, string teamId, string appId);

        Task Delete(AuthenticationType authType, string teamId);

        Task<Team> Update(AuthenticationType authType, string teamId, string displayName);
    }

    internal class GraphTeamsService : IGraphTeamsService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphTeamsService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphServiceClient> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<string> Create(AuthenticationType authType, string displayName, string description, string visibility)
        {
            Guard.Against.NullOrEmpty(displayName, nameof(displayName));

            using var _ = _logger.BeginScope(nameof(Create));

            try
            {
                _logger.LogInformation("Creating new team, {displayName}", displayName);

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var content = new Dictionary<string, string>
                {
                    { "displayName", displayName },
                    { "description", description },
                    { "visibility", visibility },
                    { "template@odata.bind", "https://graph.microsoft.com/v1.0/teamsTemplates('standard')" }
                };

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://graph.microsoft.com/v1.0/teams")
                {
                    Content = new StringContent(content.ToJsonString(), Encoding.UTF8, "application/json")
                };

                await client.AuthenticationProvider.AuthenticateRequestAsync(request);

                var response = await PollyPolicies.WaitAndRetryHttpResponseMessageAsync(logger: _logger)
                    .ExecuteAsync(_ => client.HttpProvider.SendAsync(request),
                        new Context(nameof(Create)));

                _logger.LogInformation("Graph API responded with status {statusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to create team.", new Exception(await response.Content.ReadAsStringAsync()));
                }

                var operation = await PollyPolicies.WaitAndRetryAsync<TeamsAsyncOperation>(
                    operation => operation == null ||
                        operation.Status == TeamsAsyncOperationStatus.NotStarted ||
                        operation.Status == TeamsAsyncOperationStatus.InProgress,
                    retryCount: 20,
                    logger: _logger)
                    .ExecuteAsync(_ => GetTeamsAsyncOperation(client, response.Headers.Location),
                        new Context(nameof(Create)));

                if (operation != null && operation.Status == TeamsAsyncOperationStatus.Succeeded)
                {
                    _logger.LogInformation("Team was successfully created with id {targetResourceId}.", operation.TargetResourceId);
                    return operation.TargetResourceId;
                }
                else
                {
                    if (operation == null)
                    {
                        _logger.LogInformation("Team was not successfully created, there was an unknown error");
                    }
                    else
                    {
                        _logger.LogInformation("Team was not successfully created, {status}, {error}", operation.Status, operation.Error);
                    }

                    return null;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {displayName}, {description}, {visibility}",
                    authType, displayName, description, visibility);
                throw;
            }
        }

        public async Task<Team> Update(AuthenticationType authType, string teamId, string displayName)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(displayName, nameof(displayName));

            using var _ = _logger.BeginScope(nameof(Update));

            try
            {
                _logger.LogInformation("Updating display name of team, '{teamId}' to '{displayName}'", teamId, displayName);

                var team = new Team { Id = teamId, DisplayName = displayName };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Request().UpdateAsync(team),
                        new Context(nameof(Update)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {displayName}",
                    authType, teamId, displayName);
                throw;
            }
        }

        public async Task Delete(AuthenticationType authType, string teamId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            using var _ = _logger.BeginScope(nameof(Delete));

            try
            {
                _logger.LogInformation("Deleting team, {teamId}", teamId);

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Groups[teamId].Request().DeleteAsync(),
                        new Context(nameof(Delete)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}",
                    authType, teamId);
                throw;
            }
        }

        public async Task<TeamsAppInstallation> InstallApp(AuthenticationType authType, string teamId, string appId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(appId, nameof(appId));

            using var _ = _logger.BeginScope(nameof(InstallApp));

            try
            {
                var app = new TeamsAppInstallation()
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"teamsApp@odata.bind", $"https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/{appId}"}
                    }
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].InstalledApps.Request().AddAsync(app),
                        new Context(nameof(InstallApp)));
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning("An app with id '{appId}' was already installed in team '{teamId}'", appId, teamId);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {appId}",
                    authType, teamId, appId);
                throw;
            }

            return await GetInstalledApp(authType, teamId, appId);
        }

        private async Task<TeamsAppInstallation> GetInstalledApp(AuthenticationType authType, string teamId, string appId)
        {
            using var _ = _logger.BeginScope(nameof(GetInstalledApp));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var allApps = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].InstalledApps.Request().Expand("teamsAppDefinition").GetAsync(),
                        new Context(nameof(GetInstalledApp)));

                var pageIterator = PageIterator<TeamsAppInstallation>.CreatePageIterator(client, allApps, (i) => { return true; });

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => pageIterator.IterateAsync(),
                        new Context(nameof(GetInstalledApp)));

                return allApps.CurrentPage.FirstOrDefault(a => a.TeamsAppDefinition != null && a.TeamsAppDefinition.TeamsAppId == appId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {appId}", teamId, appId);
                throw;
            }
        }

        private async Task<TeamsAsyncOperation> GetTeamsAsyncOperation(GraphServiceClient client, Uri uri)
        {
            using var _ = _logger.BeginScope(nameof(GetTeamsAsyncOperation));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0{uri}");

                await client.AuthenticationProvider.AuthenticateRequestAsync(request);

                var response = await client.HttpProvider.SendAsync(request);

                return JsonConvert.DeserializeObject<TeamsAsyncOperation>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {uri}", uri);
                throw;
            }
        }
    }
}
