using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphTeamsChannelsService
    {
        Task<ITeamChannelsCollectionPage> TeamChannelsList(AuthenticationType authType, string teamId);

        Task<Channel> TeamChannelGet(AuthenticationType authType, string teamId, string channelId);

        Task<Channel> TeamChannelCreate(AuthenticationType authType, string teamId, string displayName, bool isFavoriteByDefault, ChannelMembershipType membershipType);

        Task<IEnumerable<Channel>> TeamChannelCreateBulk(AuthenticationType authType, string teamId, IEnumerable<Channel> channels);

        Task<TeamsTab> TeamChannelTabsCreate(AuthenticationType authType, string teamId, string channelId, string appId, string displayName, string contentUrl);
    }

    internal class GraphTeamsChannelsService : IGraphTeamsChannelsService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphTeamsChannelsService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphTeamsChannelsService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<ITeamChannelsCollectionPage> TeamChannelsList(AuthenticationType authType, string teamId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            using var _ = _logger.BeginScope(nameof(TeamChannelsList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels.Request().GetAsync(),
                        new Context(nameof(TeamChannelsList)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}", authType, teamId);
                throw;
            }
        }

        public async Task<Channel> TeamChannelGet(AuthenticationType authType, string teamId, string channelId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(channelId, nameof(channelId));

            using var _ = _logger.BeginScope(nameof(TeamChannelGet));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels[channelId].Request().GetAsync(),
                        new Context(nameof(TeamChannelGet)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {channelId}", authType, teamId, channelId);
                throw;
            }
        }

        public async Task<Channel> TeamChannelCreate(
            AuthenticationType authType, string teamId, string displayName, bool isFavoriteByDefault, ChannelMembershipType membershipType)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(displayName, nameof(displayName));

            using var _ = _logger.BeginScope(nameof(TeamChannelCreate));

            try
            {
                _logger.LogInformation("Creating new channel '{displayName}' for team {teamId}", displayName, teamId);

                var request = new Channel()
                {
                    DisplayName = displayName,
                    MembershipType = membershipType,
                    IsFavoriteByDefault = isFavoriteByDefault
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var channel = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels.Request().AddAsync(request),
                        new Context(nameof(TeamChannelCreate)));

                _logger.LogInformation("Channel was successfully created with {channelId}", channel.Id);

                return channel;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {displayName}, {isFavoriteByDefault}, {membershipType}",
                    authType, teamId, displayName, isFavoriteByDefault, membershipType);
                throw;
            }
        }

        public async Task<IEnumerable<Channel>> TeamChannelCreateBulk(AuthenticationType authType, string teamId, IEnumerable<Channel> channels)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            if (channels.IsEmpty())
            {
                return Enumerable.Empty<Channel>();
            }

            using var _ = _logger.BeginScope(nameof(TeamChannelCreate));

            try
            {
                _logger.LogInformation("Creating {channelsCount} new channels for team '{teamId}'", channels.Count(), teamId);

                var createdChannels = new List<Channel>();
                int successCount = 0, failCount = 0;

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var channelsChunk in channels.Chunk(Constants.MaxBatchSize))
                {
                    var batchRequestContent = new BatchRequestContent();

                    foreach (var channel in channelsChunk)
                    {
                        var httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Post,
                            $"https://graph.microsoft.com/v1.0/teams/{teamId}/channels")
                        {
                            Content = new StringContent(channel.ToJsonString(), Encoding.UTF8, "application/json")
                        };

                        batchRequestContent.AddBatchRequestStep(
                            new BatchRequestStep(Guid.NewGuid().ToString(), httpRequestMessage));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequestContent),
                            new Context(nameof(TeamChannelCreateBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        if (response.Value.IsSuccessStatusCode)
                        {
                            createdChannels.Add(JsonConvert.DeserializeObject<Channel>(await response.Value.Content.ReadAsStringAsync()));
                            successCount++;
                        }
                        else
                        {
                            _logger.LogError("Unable to create channel for team '{teamId}', {response}",
                                response.Key, teamId, await response.Value.Content.ReadAsStringAsync());
                            failCount++;
                        }
                    }
                }

                _logger.LogInformation("Successfully created {successCount} channels for team '{teamId}'", successCount, teamId);

                if (failCount > 0)
                {
                    _logger.LogWarning("Failed to create {failCount} channels for team '{teamId}'", failCount, teamId);
                }

                return createdChannels;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channels}",
                    authType, teamId, channels.ToJsonString());
                throw;
            }
        }

        public async Task<TeamsTab> TeamChannelTabsCreate(
            AuthenticationType authType, string teamId, string channelId, string appId, string displayName, string contentUrl)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(channelId, nameof(channelId));
            Guard.Against.NullOrEmpty(appId, nameof(appId));
            Guard.Against.NullOrEmpty(displayName, nameof(displayName));
            Guard.Against.NullOrEmpty(contentUrl, nameof(contentUrl));

            using var _ = _logger.BeginScope(nameof(TeamChannelTabsCreate));

            try
            {
                var teamsTab = new TeamsTab()
                {
                    DisplayName = displayName,
                    Configuration = new TeamsTabConfiguration { ContentUrl = contentUrl },
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"teamsApp@odata.bind", $"https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/{appId}"}
                    }
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels[channelId].Tabs.Request().AddAsync(teamsTab),
                        new Context(nameof(TeamChannelTabsCreate)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {channelId}, {appId}, {displayName}, {contentUrl}",
                    authType, teamId, channelId, appId, displayName, displayName, contentUrl);
                throw;
            }
        }
    }
}
