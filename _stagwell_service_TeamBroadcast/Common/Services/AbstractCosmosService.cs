using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface ICosmosService
    {
        Task<T> GetItem<T>(string containerName, string id, string partitionKey = default);

        Task<T> GetItem<T>(string containerName, QueryDefinition query, string partitionKey = default);

        Task<IEnumerable<T>> GetList<T>(string containerName, QueryDefinition query);

        Task<ItemResponse<T>> CreateItem<T>(string containerName, T item, string partitionKey = default);

        Task<ItemResponse<T>> UpsertItem<T>(string containerName, T item, string partitionKey = default);

        Task<ItemResponse<T>> DeleteItem<T>(string containerName, string id, string partitionKey = default);
    }

    public abstract class AbstractCosmosService : ICosmosService
    {
        private readonly ILogger _logger;

        protected AbstractCosmosService(
            string databaseName,
            ICosmosClientFactory cosmosClientFactory,
            ILogger logger)
        {
            Guard.Against.NullOrEmpty(databaseName, nameof(databaseName));
            Guard.Against.Null(cosmosClientFactory, nameof(cosmosClientFactory));
            _logger = Guard.Against.Null(logger, nameof(logger));
            Database = cosmosClientFactory.CreateClient().GetDatabase(databaseName);
        }

        protected Database Database { get; }

        public Container GetContainer(string containerName) => Database.GetContainer(containerName);

        public async Task<T> GetItem<T>(string containerName, string id, string partitionKey = default)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.NullOrEmpty(id, nameof(id));

            using var _ = _logger.BeginScope(nameof(GetItem));

            try
            {
                var container = GetContainer(containerName);

                var response = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => container.ReadItemAsync<T>(id, new PartitionKey(partitionKey)),
                        new Context(nameof(GetItem)));

                return response.Resource;
            }
            catch (CosmosException exception)
            {
                if (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Item not found, {containerName}, {id}, {partitionKey}", containerName, id, partitionKey);
                    return default;
                }

                _logger.LogError(exception, "Failed, {containerName}, {id}, {partitionKey}", containerName, id, partitionKey);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {id}, {partitionKey}", containerName, id, partitionKey);
                throw;
            }
        }

        public async Task<T> GetItem<T>(string containerName, QueryDefinition query, string partitionKey = default)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.Null(query, nameof(query));

            using var _ = _logger.BeginScope(nameof(GetItem));

            try
            {
                var container = GetContainer(containerName);

                var requestOptions = new QueryRequestOptions()
                {
                    PartitionKey = partitionKey.ToPartitionKey()
                };

                using var iterator = PollyPolicies.WaitAndRetry(logger: _logger)
                    .Execute(_ => container.GetItemQueryIterator<T>(query, requestOptions: requestOptions),
                        new Context(nameof(GetItem)));

                if (iterator.HasMoreResults)
                {
                    var result = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => iterator.ReadNextAsync(),
                            new Context(nameof(GetItem)));

                    return result.FirstOrDefault();
                }

                return default;
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {query}, {partitionKey}", containerName, query.QueryText, partitionKey);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {query}, {partitionKey}", containerName, query.QueryText, partitionKey);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetList<T>(string containerName, QueryDefinition query)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.Null(query, nameof(query));

            using var _ = _logger.BeginScope(nameof(GetList));

            try
            {
                var items = new List<T>();
                var container = GetContainer(containerName);

                using var iterator = PollyPolicies.WaitAndRetry(logger: _logger)
                    .Execute(_ => container.GetItemQueryIterator<T>(query),
                        new Context(nameof(GetList)));

                while (iterator.HasMoreResults)
                {
                    var result = await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                        .ExecuteAsync(_ => iterator.ReadNextAsync(),
                            new Context(nameof(GetList)));

                    items.AddRange(result);
                }

                return items;
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {query}", containerName, query.QueryText);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {query}", containerName, query.QueryText);
                throw;
            }
        }

        public async Task<ItemResponse<T>> CreateItem<T>(string containerName, T item, string partitionKey = default)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.Null(item, nameof(item));

            using var _ = _logger.BeginScope(nameof(CreateItem));

            try
            {
                var container = GetContainer(containerName);
                return await container.CreateItemAsync(item, partitionKey.ToPartitionKey());
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {item}", containerName, item.ToJsonString());
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed, {containerName}, {item}", containerName, item.ToJsonString());
                throw;
            }
        }

        public async Task<ItemResponse<T>> UpsertItem<T>(string containerName, T item, string partitionKey = default)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.Null(item, nameof(item));

            using var _ = _logger.BeginScope(nameof(UpsertItem));

            try
            {
                var container = GetContainer(containerName);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => container.UpsertItemAsync(item, partitionKey.ToPartitionKey()),
                        new Context(nameof(UpsertItem)));
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, "UpsertItem: failed, {containerName}, {item}", containerName, item.ToJsonString());
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "UpsertItem: failed, {containerName}, {item}", containerName, item.ToJsonString());
                throw;
            }
        }

        public async Task<ItemResponse<T>> DeleteItem<T>(string containerName, string id, string partitionKey = default)
        {
            Guard.Against.NullOrEmpty(containerName, nameof(containerName));
            Guard.Against.NullOrEmpty(id, nameof(id));

            using var _ = _logger.BeginScope(nameof(DeleteItem));

            try
            {
                var container = GetContainer(containerName);

                return await PollyPolicies.WaitAndRetryAsync(logger: _logger)
                    .ExecuteAsync(_ => container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey)),
                        new Context(nameof(DeleteItem)));
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, "DeleteItem: failed, {containerName}, {id}", containerName, id);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DeleteItem: failed, {containerName}, {id}", containerName, id);
                throw;
            }
        }
    }
}
