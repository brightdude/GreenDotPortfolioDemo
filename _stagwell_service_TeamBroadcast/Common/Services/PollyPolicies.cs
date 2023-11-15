using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Breezy.Muticaster
{
    internal static class PollyPolicies
    {
        public static readonly HttpStatusCode[] _retriableStatusCodes = new[]
        {
           HttpStatusCode.RequestTimeout,
           HttpStatusCode.InternalServerError,
           HttpStatusCode.BadGateway,
           HttpStatusCode.ServiceUnavailable,
           HttpStatusCode.GatewayTimeout,
           HttpStatusCode.TooManyRequests
        };
       
        public static AsyncPolicy WaitAndRetryAsync(int retryCount = 5, ILogger logger = default)
        {
            logger ??= NullLogger.Instance;
            return Policy
                .Handle<CosmosException>(e => _retriableStatusCodes.Contains(e.StatusCode))
                .Or<ServiceException>(e => _retriableStatusCodes.Contains(e.StatusCode))
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, _, retryCount, context) =>
                    {
                        logger.LogInformation(exception, "Retry {retryCount} of {policyKey} at {operationKey}",
                            retryCount, context.PolicyKey, context.OperationKey);
                    })
                .WithPolicyKey(nameof(WaitAndRetryAsync));
        }

        public static AsyncPolicy<T> WaitAndRetryAsync<T>(Predicate<T> predicate, int retryCount = 5, ILogger logger = default)
        {
            logger ??= NullLogger.Instance;
            return Policy
                .HandleResult<T>(r => predicate(r))
                .Or<CosmosException>(e => _retriableStatusCodes.Contains(e.StatusCode))
                .Or<ServiceException>(e => _retriableStatusCodes.Contains(e.StatusCode))
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, _, retryCount, context) =>
                {
                    if (result.Exception is not null)
                    {
                        logger.LogInformation(result.Exception, "Retry {retryCount} of {policyKey} at {operationKey}",
                            retryCount, context.PolicyKey, context.OperationKey);
                    }
                    else
                    {
                        logger.LogInformation("Retry {retryCount} of {policyKey} at {operationKey}, {result}",
                            retryCount, context.PolicyKey, context.OperationKey, result.ToJsonString());
                    }
                })
                .WithPolicyKey(nameof(WaitAndRetryAsync));
        }

        public static Policy WaitAndRetry(int retryCount = 5, ILogger logger = default)
        {
            return Policy
                .Handle<CosmosException>(ex => _retriableStatusCodes.Contains(ex.StatusCode))
                .Or<ServiceException>(e => _retriableStatusCodes.Contains(e.StatusCode))
                .WaitAndRetry(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, _, retryCount, context) =>
                    {
                        logger.LogInformation(exception, "Retry {retryCount} of {policyKey} at {operationKey}",
                            retryCount, context.PolicyKey, context.OperationKey);
                    })
                .WithPolicyKey(nameof(WaitAndRetryAsync));
        }       

        internal static AsyncPolicy<HttpResponseMessage> WaitAndRetryHttpResponseMessageAsync(int retryCount = 5, ILogger logger = default)
        {
            return WaitAndRetryAsync<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode && _retriableStatusCodes.Contains(r.StatusCode), retryCount, logger);
        }
    }
}
