using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphUsersService
    {
        Task<Microsoft.Graph.User> UserGet(AuthenticationType authType, string userId);

        Task<Invitation> CreateInvitation(AuthenticationType authType, string email, string infoTeamGeneralChannelId, string everyoneTeamId, string tenantId);

        Task<Microsoft.Graph.User> UserUpdate(AuthenticationType authType, string userId, Microsoft.Graph.User user);

        Task<bool> UserDelete(AuthenticationType authType, string userId);

        Task<Microsoft.Graph.User> UserCreate(AuthenticationType authType, Microsoft.Graph.User user);

        Task SendMail(AuthenticationType authType, string sendingUserId, string toEmailAddress, string subject, string content);
    }

    internal class GraphUsersService : IGraphUsersService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphUsersService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphUsersService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<Microsoft.Graph.User> UserGet(AuthenticationType authType, string userId)
        {
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            using var _ = _logger.BeginScope(nameof(UserGet));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Users[userId].Request().GetAsync(),
                        new Context(nameof(UserGet)));
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User with id '{userId}' was not found", userId);
                    return null;
                }
                else
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {userId}",
                    authType, userId);
                throw;
            }
        }

        public async Task<Invitation> CreateInvitation(
            AuthenticationType authType, string email, string infoTeamGeneralChannelId, string everyoneTeamId, string tenantId)
        {
            Guard.Against.NullOrEmpty(email, nameof(email));
            Guard.Against.NullOrEmpty(infoTeamGeneralChannelId, nameof(infoTeamGeneralChannelId));
            Guard.Against.NullOrEmpty(everyoneTeamId, nameof(everyoneTeamId));
            Guard.Against.NullOrEmpty(tenantId, nameof(tenantId));

            using var _ = _logger.BeginScope(nameof(CreateInvitation));

            try
            {
                var invitationRedirectUrl = $"https://teams.microsoft.com/l/channel/{infoTeamGeneralChannelId}/General?groupId={everyoneTeamId}&tenantId={tenantId}";
                var invitationRequest = new Invitation()
                {
                    InviteRedirectUrl = invitationRedirectUrl,
                    InvitedUserEmailAddress = email,
                    SendInvitationMessage = false
                };
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Invitations.Request().AddAsync(invitationRequest),
                        new Context(nameof(CreateInvitation)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {email}, {infoTeamGeneralChannelId}, {everyoneTeamId}, {tenantId}",
                    authType, email, infoTeamGeneralChannelId, everyoneTeamId, tenantId);
                throw;
            }
        }

        public async Task<Microsoft.Graph.User> UserUpdate(AuthenticationType authType, string userId, Microsoft.Graph.User user)
        {
            Guard.Against.NullOrEmpty(userId, nameof(userId));
            Guard.Against.Null(user, nameof(user));

            using var _ = _logger.BeginScope(nameof(UserUpdate));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Users[userId].Request().UpdateAsync(user),
                        new Context(nameof(UserUpdate)));
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Unable to find user with id of '{userId}'. User was not updated", userId);
                    return null;
                }
                else
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {userId}, {user}",
                    authType, userId, user.ToJsonString());
                throw;
            }
        }

        public async Task<bool> UserDelete(AuthenticationType authType, string userId)
        {
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            using var _ = _logger.BeginScope(nameof(UserDelete));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Users[userId].Request().DeleteAsync(),
                        new Context(nameof(UserDelete)));

                return true;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Unable to find user with id of '{userId}'. User was not deleted", userId);
                    return true;
                }
                else
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {userId}",
                    authType, userId);
                throw;
            }
        }

        public async Task<Microsoft.Graph.User> UserCreate(AuthenticationType authType, Microsoft.Graph.User user)
        {
            Guard.Against.Null(user, nameof(user));

            using var _ = _logger.BeginScope(nameof(UserCreate));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Users.Request().AddAsync(user),
                        new Context(nameof(UserCreate)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {user}",
                    authType, user.ToJsonString());
                throw;
            }
        }

        public async Task SendMail(AuthenticationType authType, string sendingUserId, string toEmailAddress, string subject, string content)
        {
            Guard.Against.Null(sendingUserId, nameof(sendingUserId));
            Guard.Against.Null(toEmailAddress, nameof(toEmailAddress));

            using var _ = _logger.BeginScope(nameof(SendMail));

            try
            {
                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = content
                    },
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress() { Address = toEmailAddress }
                        }
                    }
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Users[sendingUserId].SendMail(message, false).Request().PostAsync(),
                        new Context(nameof(SendMail)));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {sendingUserId}, {toEmailAddress}, {subject}, {content}",
                    authType, sendingUserId, toEmailAddress, subject, content);
                throw;
            }
        }
    }
}
