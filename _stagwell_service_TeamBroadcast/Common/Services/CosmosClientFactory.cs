using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Breezy.Muticaster
{
    public interface ICosmosClientFactory
    {
        CosmosClient CreateClient();
    }

    internal class CosmosClientFactory : ICosmosClientFactory
    {
        private readonly IOptionsMonitor<CosmosOptions> _options;       
        private readonly IHttpClientFactory _httpClientFactory;        

        public CosmosClientFactory(
            IOptionsMonitor<CosmosOptions> options,          
            IHttpClientFactory httpClientFactory)
        {           
            _options = Guard.Against.Null(options, nameof(options));
            _httpClientFactory = Guard.Against.Null(httpClientFactory, nameof(httpClientFactory));           
        }

        public CosmosClient CreateClient()
        {
            var options = _options.CurrentValue;

            var clientBuilder = new CosmosClientBuilder(options.ConnectionString)
                .WithApplicationRegion(options.ApplicationRegion)
                .WithBulkExecution(true)
                .WithHttpClientFactory(_httpClientFactory.CreateClient);

            return clientBuilder.Build();
        }
    }
}
