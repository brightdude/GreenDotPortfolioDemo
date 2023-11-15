using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphChatsService
    {
        Task<Microsoft.Graph.ChatMessage> ChatMessagesCreate(AuthenticationType authType, string threadId, string content);

        Task<IEnumerable<ConversationMember>> ChatMembersList(AuthenticationType authType, string threadId);
    }

    internal class GraphChatsService : IGraphChatsService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphChatsService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphChatsService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<Microsoft.Graph.ChatMessage> ChatMessagesCreate(AuthenticationType authType, string threadId, string content)
        {
            Guard.Against.NullOrEmpty(threadId, nameof(threadId));
            Guard.Against.NullOrEmpty(content, nameof(content));

            using var _ = _logger.BeginScope(nameof(ChatMessagesCreate));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);
                var chatMessage = new Microsoft.Graph.ChatMessage { Body = new ItemBody { ContentType = BodyType.Html, Content = content } };

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Me.Chats[threadId].Messages.Request().AddAsync(chatMessage),
                        new Context(nameof(ChatMessagesCreate)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {threadId}, {content}", authType, threadId, content);
                throw;
            }
        }

        public async Task<IEnumerable<ConversationMember>> ChatMembersList(AuthenticationType authType, string threadId)
        {
            Guard.Against.NullOrEmpty(threadId, nameof(threadId));

            using var _ = _logger.BeginScope(nameof(ChatMembersList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);
                var members = new List<ConversationMember>();

                var page = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Me.Chats[threadId].Members.Request().GetAsync(),
                        new Context(nameof(ChatMembersList)));

                if (!page.IsEmpty())
                {
                    members.AddRange(page.CurrentPage);

                    while (page.NextPageRequest != null)
                    {
                        page = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                            .ExecuteAsync(_ => page.NextPageRequest.GetAsync(),
                                new Context(nameof(ChatMembersList)));

                        members.AddRange(page.CurrentPage);
                    }
                }
                return members;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {threadId}", authType, threadId);
                throw;
            }
        }
    }
}
