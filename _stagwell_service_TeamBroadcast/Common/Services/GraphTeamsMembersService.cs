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
    public interface IGraphTeamsMembersService
    {
        Task<ITeamMembersCollectionPage> TeamMembersList(AuthenticationType authType, string teamId);

        Task<ITeamMembersCollectionPage> TeamMembersList(AuthenticationType authType, string teamId, string userId);

        Task<ConversationMember> TeamMemberCreate(AuthenticationType authType, string teamId, string userId);

        Task TeamMemberCreateBulk(AuthenticationType authType, string teamId, IEnumerable<string> userIds);

        Task TeamMemberDelete(AuthenticationType authType, string teamId, string userId);

        Task<int> TeamMemberDeleteBulk(AuthenticationType authType, string teamId, IEnumerable<string> userIds);
    }

    internal class GraphTeamsMembersService : IGraphTeamsMembersService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphTeamsMembersService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphTeamsMembersService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<ITeamMembersCollectionPage> TeamMembersList(AuthenticationType authType, string teamId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            using var _ = _logger.BeginScope(nameof(TeamMembersList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Members.Request().GetAsync(),
                        new Context(nameof(TeamMembersList)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}", authType, teamId);
                throw;
            }
        }

        public async Task<ITeamMembersCollectionPage> TeamMembersList(AuthenticationType authType, string teamId, string userId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            using var _ = _logger.BeginScope(nameof(TeamMembersList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Members.Request()
                        .Filter($"(microsoft.graph.aadUserConversationMember/userId eq '{userId}')").GetAsync(),
                        new Context(nameof(TeamMembersList)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {userId}", authType, teamId, userId);
                throw;
            }
        }

        public async Task<ConversationMember> TeamMemberCreate(AuthenticationType authType, string teamId, string userId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            using var _ = _logger.BeginScope(nameof(TeamMemberCreate));

            try
            {
                _logger.LogInformation("Adding user '{userId}' to team {teamId}", userId, teamId);

                var request = new AadUserConversationMember()
                {
                    UserId = userId,
                    Roles = Array.Empty<string>(),
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')"},
                        {"@odata.type", "#microsoft.graph.aadUserConversationMember"}
                    }
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var member = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Members.Request().AddAsync(request),
                        new Context(nameof(TeamMemberCreate)));

                _logger.LogInformation("User '{userId}' was successfully added", userId);
                return member;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {teamId}, {userId}", authType, teamId, userId);
                throw;
            }
        }

        public async Task TeamMemberCreateBulk(AuthenticationType authType, string teamId, IEnumerable<string> userIds)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            if (userIds.IsEmpty())
            {
                return;
            }

            using var _ = _logger.BeginScope(nameof(TeamMemberCreateBulk));

            try
            {
                _logger.LogInformation("Adding {userIdsCount} users to team '{TeamId}'", userIds.Count(), teamId);

                int successCount = 0, failCount = 0;

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var idsChunk in userIds.Chunk(Constants.MaxBatchSize))
                {
                    var batchRequestContent = new BatchRequestContent();

                    foreach (var userId in idsChunk)
                    {
                        var request = new Dictionary<string, object>()
                        {
                            { "@odata.type", "#microsoft.graph.aadUserConversationMember" },
                            { "roles", Array.Empty<string>() },
                            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')" }
                        };

                        var httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Post,
                            $"https://graph.microsoft.com/v1.0/teams/{teamId}/members")
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
                        };

                        batchRequestContent.AddBatchRequestStep(new BatchRequestStep(userId, httpRequestMessage));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequestContent),
                            new Context(nameof(TeamMemberCreateBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        if (response.Value.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            _logger.LogError("Unable to add user '{userId}' to team '{teamId}', {response}",
                                response.Key, teamId, await response.Value.Content.ReadAsStringAsync());
                            failCount++;
                        }
                    }
                }

                _logger.LogInformation("Successfully added {successCount} users to team '{teamId}'", successCount, teamId);

                if (failCount > 0)
                {
                    _logger.LogWarning("Failed to add {failCount} users to team '{TeamId}'", failCount, teamId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {userIds}",
                    authType, teamId, userIds.ToJsonString());
                throw;
            }
        }

        public async Task TeamMemberDelete(AuthenticationType authType, string teamId, string userId)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            using var _ = _logger.BeginScope(nameof(TeamMemberDelete));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Teams[teamId].Members[userId].Request().DeleteAsync(),
                        new Context(nameof(TeamMemberDelete)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {memberId}",
                    authType, teamId, userId);
                throw;
            }
        }

        public async Task<int> TeamMemberDeleteBulk(AuthenticationType authType, string teamId, IEnumerable<string> userIds)
        {
            Guard.Against.NullOrEmpty(teamId, nameof(teamId));

            if (userIds.IsEmpty())
            {
                return 0;
            }

            using var _ = _logger.BeginScope(nameof(TeamMemberDeleteBulk));

            try
            {
                int successCount = 0, failCount = 0;

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var idsChunk in userIds.Chunk((Constants.MaxBatchSize)))
                {
                    var batchRequestContent = new BatchRequestContent();

                    foreach (var userId in idsChunk)
                    {
                        var httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Delete,
                            $"https://graph.microsoft.com/v1.0/teams/{teamId}/members/{userId}");

                        batchRequestContent.AddBatchRequestStep(new BatchRequestStep(userId, httpRequestMessage));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequestContent),
                            new Context(nameof(TeamMemberDeleteBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        if (response.Value.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            _logger.LogError("Unable to delete user '{userId}' from team '{teamId}', {response}",
                                response.Key, teamId, await response.Value.Content.ReadAsStringAsync());
                            failCount++;
                        }
                    }
                }

                _logger.LogInformation("Successfully deleted {successCount} users from team '{teamId}'", successCount, teamId);

                if (failCount > 0)
                {
                    _logger.LogWarning("Failed to delete {failCount} users from team '{teamId}'", failCount, teamId);
                }

                return successCount;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {teamId}, {userIds}",
                    authType, teamId, userIds.ToJsonString());
                throw;
            }
        }
    }
}
