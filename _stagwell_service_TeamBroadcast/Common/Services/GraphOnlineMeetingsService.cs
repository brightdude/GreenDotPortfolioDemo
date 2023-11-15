using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IGraphOnlineMeetingsService
    {
        Task<OnlineMeeting> OnlineMeetingsCreateOrGet(AuthenticationType authType, string externalId, DateTime startTime, DateTime endTime, string subject);

        Task<OnlineMeeting> OnlineMeetingsUpdate(AuthenticationType authType, string meetingId, DateTime startTime, DateTime endTime, string subject);

        Task<bool> OnlineMeetingsDelete(AuthenticationType authType, string meetingId);

        Task<Dictionary<string, bool>> OnlineMeetingsDeleteBulk(AuthenticationType authType, IEnumerable<string> meetingIds);
    }

    internal class GraphOnlineMeetingsService : IGraphOnlineMeetingsService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphOnlineMeetingsService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphOnlineMeetingsService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<OnlineMeeting> OnlineMeetingsCreateOrGet(
            AuthenticationType authType, string externalId, DateTime startTime, DateTime endTime, string subject)
        {
            Guard.Against.NullOrEmpty(externalId, nameof(externalId));

            using var _ = _logger.BeginScope(nameof(OnlineMeetingsCreateOrGet));

            try
            {
                _logger.LogInformation("Creating new online meeting for '{externalId}'", externalId);

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var requestBuilder = client.Me.OnlineMeetings.CreateOrGet(
                    externalId, null, new DateTimeOffset(endTime), null, new DateTimeOffset(startTime), subject);

                var onlineMeeting = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => requestBuilder.Request().PostAsync(),
                        new Context(nameof(OnlineMeetingsCreateOrGet)));

                _logger.LogInformation("Online meeting was successfully created with id '{meetingId}'", onlineMeeting.Id);

                return onlineMeeting;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType} {externalId}, {startTime}, {endTime}, {subject}",
                    authType, externalId, startTime, endTime, subject);
                throw;
            }
        }

        public async Task<OnlineMeeting> OnlineMeetingsUpdate(
            AuthenticationType authType, string meetingId, DateTime startTime, DateTime endTime, string subject)
        {
            Guard.Against.NullOrEmpty(meetingId, nameof(meetingId));

            using var _ = _logger.BeginScope(nameof(OnlineMeetingsUpdate));

            try
            {
                _logger.LogInformation("Updating existing online meeting for '{meetingId}'", meetingId);

                var request = new OnlineMeeting
                {
                    StartDateTime = new DateTimeOffset(startTime),
                    EndDateTime = new DateTimeOffset(endTime),
                    Subject = subject
                };

                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var onlineMeeting = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Me.OnlineMeetings[meetingId].Request().UpdateAsync(request),
                        new Context(nameof(OnlineMeetingsUpdate)));

                _logger.LogInformation("Online meeting was successfully updated with id '{meetingId}'", onlineMeeting.Id);

                return onlineMeeting;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {meetingId}, {startTime}, {endTime}, {subject}",
                    authType, meetingId, startTime, endTime, subject);
                throw;
            }
        }

        public async Task<bool> OnlineMeetingsDelete(AuthenticationType authType, string meetingId)
        {
            Guard.Against.NullOrEmpty(meetingId, nameof(meetingId));

            using var _ = _logger.BeginScope(nameof(OnlineMeetingsDelete));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                var response = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Me.OnlineMeetings[meetingId].Request().DeleteResponseAsync(),
                        new Context(nameof(OnlineMeetingsDelete)));

                return response.StatusCode == HttpStatusCode.NoContent;
            }
            catch (ServiceException exception)
            {
                if (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Unable to find online meeting with id of '{meetingId}'. Meeting was not deleted", meetingId);
                    return true;
                }

                _logger.LogError(exception, "Failed, {authenticationType}, {meetingId}", authType, meetingId);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {meetingId}", authType, meetingId);
                throw;
            }
        }

        public async Task<Dictionary<string, bool>> OnlineMeetingsDeleteBulk(AuthenticationType authType, IEnumerable<string> meetingIds)
        {
            var batchResult = new Dictionary<string, bool>();

            if (meetingIds.IsEmpty())
            {
                return batchResult;
            }

            using var _ = _logger.BeginScope(nameof(OnlineMeetingsDelete));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);

                foreach (var idsChunk in meetingIds.Chunk(Constants.MaxBatchSize))
                {
                    var batchRequest = new BatchRequestContent();

                    foreach (var id in idsChunk)
                    {
                        var request = client.Me.OnlineMeetings[id].Request();
                        request.Method = HttpMethods.DELETE;

                        batchRequest.AddBatchRequestStep(
                            new BatchRequestStep(id, request.GetHttpRequestMessage()));
                    }

                    var batchResponse = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => client.Batch.Request().PostAsync(batchRequest),
                            new Context(nameof(OnlineMeetingsDeleteBulk)));

                    foreach (var response in await batchResponse.GetResponsesAsync())
                    {
                        switch (response.Value.StatusCode)
                        {
                            case HttpStatusCode.NoContent:
                                batchResult.Add(response.Key, true);
                                break;

                            case HttpStatusCode.NotFound:
                                _logger.LogWarning("Unable to find online meeting with id of '{meetingId}'. Meeting was not deleted", response.Key);
                                batchResult.Add(response.Key, true);
                                break;

                            default:
                                _logger.LogError("Unable to delete online meeting with id of '{meetingId}', {response}",
                                    response.Key, await response.Value.Content.ReadAsStringAsync());
                                batchResult.Add(response.Key, false);
                                break;
                        }
                    }
                }

                return batchResult;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}, {meetingIds}", authType, meetingIds.ToJsonString());
                throw;
            }
        }
    }
}
