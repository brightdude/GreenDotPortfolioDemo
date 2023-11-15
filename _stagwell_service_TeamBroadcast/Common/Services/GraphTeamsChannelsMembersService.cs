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
    public interface IGraphTeamsChannelsMembersService
    {
        Task<IChannelMembersCollectionPage> TeamChannelMemberList(AuthenticationType authType, string teamId, string channelId);

        Task<Microsoft.Graph.ConversationMember> TeamChannelMemberCreate(AuthenticationType authType, string teamId, string channelId, string userId);

        Task<int> TeamChannelMemberCreateBulk(AuthenticationType authType, string teamId, string channelId, IEnumerable<string> userIds);

        Task TeamChannelMemberDelete(AuthenticationType authType, string teamId, string channelId, string userId);

        Task<int> TeamChannelMemberDeleteBulk(AuthenticationType authType, string teamId, string channelId, IEnumerable<string> userIds);
    }

    internal class GraphTeamsChannelsMembersService : IGraphTeamsChannelsMembersService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphTeamsChannelsMembersService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphTeamsChannelsMembersService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<IChannelMembersCollectionPage> TeamChannelMemberList(
            AuthenticationType authType, string teamId, string channelId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(channelId, nameof(channelId));

            using var _ = _logger.BeginScope(nameof(TeamChannelMemberList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels[channelId].Members.Request().GetAsync(),
                        new Context(nameof(TeamChannelMemberList)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channelId}", authType, teamId, channelId);
                throw;
            }
        }

        public async Task<Microsoft.Graph.ConversationMember> TeamChannelMemberCreate(
            AuthenticationType authType, string teamId, string channelId, string userId)
        {
            using var _ = _logger.BeginScope(nameof(TeamChannelMemberCreate));

            try
            {
                var conversationMember = new AadUserConversationMember
                {
                    UserId = userId,
                    Roles = Array.Empty<string>(),
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')"}
                    }
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels[channelId].Members.Request().AddAsync(conversationMember),
                        new Context(nameof(TeamChannelMemberCreate)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channelId}, {userId}",
                    authType, teamId, channelId, userId);
                throw;
            }
        }

        public async Task<int> TeamChannelMemberCreateBulk(
            AuthenticationType authType, string teamId, string channelId, IEnumerable<string> userIds)
        {
            if (userIds.IsEmpty())
            {
                return 0;
            }

            using var _ = _logger.BeginScope(nameof(TeamChannelMemberCreateBulk));

            try
            {
                _logger.LogInformation("Bulk adding {userIdsCount} users to channel '{channelId}' in team '{teamId}'",
                    userIds.Count(), channelId, teamId);

                int successCount = 0, failCount = 0;

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var idsChunk in userIds.Chunk(Constants.MaxBatchSize))
                {
                    var batchRequest = new BatchRequestContent();

                    foreach (var userId in idsChunk)
                    {
                        var request = new Dictionary<string, object>()
                        {
                            { "@odata.type", "#microsoft.graph.aadUserConversationMember" },
                            { "roles", Array.Empty<string>() },
                            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')" }
                        };

                        var requestMessage = new HttpRequestMessage(
                            HttpMethod.Post,
                            $"https://graph.microsoft.com/v1.0/teams/{teamId}/channels/{channelId}/members")
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
                        };

                        batchRequest.AddBatchRequestStep(new BatchRequestStep(userId, requestMessage));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequest),
                            new Context(nameof(TeamChannelMemberCreateBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        if (response.Value.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            _logger.LogError("Unable to add user '{userId}' to channel '{channelId}' in team '{teamId}', {response}",
                                response.Key, channelId, teamId, await response.Value.Content.ReadAsStringAsync());
                            failCount++;
                        }
                    }
                }

                _logger.LogInformation("Successfully added {successCount} users to channel '{channelId}' in team '{teamId}'",
                    successCount, channelId, teamId);

                if (failCount > 0)
                {
                    _logger.LogWarning("Failed to add {failCount} users to channel '{channelId}' in team '{teamId}'",
                        failCount, channelId, teamId);
                }

                return successCount;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channelId}, {userIds}",
                    authType, teamId, channelId, userIds.ToJsonString());
                throw;
            }
        }

        public async Task TeamChannelMemberDelete(AuthenticationType authType, string teamId, string channelId, string userId)
        {
            using var _ = _logger.BeginScope(nameof(TeamChannelMemberDelete));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Channels[channelId].Members[userId].Request().DeleteAsync(),
                        new Context(nameof(TeamChannelMemberDelete)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channelIds}, {userId}",
                    authType, teamId, channelId, userId);
                throw;
            }
        }

        public async Task<int> TeamChannelMemberDeleteBulk(
            AuthenticationType authType, string teamId, string channelId, IEnumerable<string> userIds)
        {
            if (userIds.IsEmpty())
            {
                return 0;
            }

            using var _ = _logger.BeginScope(nameof(TeamChannelMemberDeleteBulk));

            try
            {
                _logger.LogInformation("Bulk deleting {userIdsCount} users to channel '{channelId}' in team '{teamId}'",
                    userIds.Count(), channelId, teamId);

                int successCount = 0, failCount = 0;

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var idsChunk in userIds.Chunk(Constants.MaxBatchSize))
                {
                    var batchRequest = new BatchRequestContent();

                    foreach (var userId in idsChunk)
                    {
                        var httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Delete,
                            $"https://graph.microsoft.com/v1.0/teams/{teamId}/channels/{channelId}/members/{userId}");

                        batchRequest.AddBatchRequestStep(new BatchRequestStep(userId, httpRequestMessage));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequest),
                            new Context(nameof(TeamChannelMemberDeleteBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        if (response.Value.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            _logger.LogError("Unable to delete user '{userId}' to channel '{channelId}' in team '{teamId}', {response}",
                                response.Key, channelId, teamId, await response.Value.Content.ReadAsStringAsync());
                            failCount++;
                        }
                    }
                }

                _logger.LogInformation("Successfully deleted {successCount} users from channel '{channelId}' in team '{teamId}'",
                    successCount, channelId, teamId);

                if (failCount > 0)
                {
                    _logger.LogWarning("Failed to delete {failCount} users from channel '{channelId}' in team '{teamId}'", 
                        failCount, channelId, teamId);
                }

                return successCount;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {channelId}, {userIds}",
                    authType, teamId, channelId, userIds.ToJsonString());
                throw;
            }
        }
    }
}
