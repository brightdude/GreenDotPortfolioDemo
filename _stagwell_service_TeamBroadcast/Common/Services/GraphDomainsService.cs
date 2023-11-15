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
    public interface IGraphDomainsService
    {
        Task<IEnumerable<Domain>> DomainsList(AuthenticationType authType);
    }

    internal class GraphDomainsService : IGraphDomainsService
    {
        private readonly IOptionsMonitor<CredentialOptions> _options;
        private readonly IGraphServiceClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GraphDomainsService(
            IOptionsMonitor<CredentialOptions> options,
            IGraphServiceClientFactory clientFactory,
            ILogger<GraphDomainsService> logger)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _clientFactory = Guard.Against.Null(clientFactory, nameof(clientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        public async Task<IEnumerable<Domain>> DomainsList(AuthenticationType authType)
        {
            using var _ = _logger.BeginScope(nameof(DomainsList));

            try
            {
                var client = _clientFactory.GetClient(_options.CurrentValue, authType);
                var domains = new List<Domain>();

                var page = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => client.Domains.Request().Select(x => new { x.Id, x.IsDefault }).GetAsync(),
                        new Context(nameof(DomainsList)));

                if (!page.IsEmpty())
                {
                    domains.AddRange(page.CurrentPage);

                    while (page.NextPageRequest != null)
                    {
                        page = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                            .ExecuteAsync(_ => page.NextPageRequest.GetAsync(),
                                new Context(nameof(DomainsList)));

                        domains.AddRange(page.CurrentPage);
                    }
                }
                return domains;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {authenticationType}", authType);
                throw;
            }
        }
    }
}
