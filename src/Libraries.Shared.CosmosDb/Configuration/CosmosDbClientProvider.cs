using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace Libraries.Shared.CosmosDb.Configuration;

public sealed class CosmosDbClientProvider(CosmosDbConfigurator configurator)
{
    private readonly ConcurrentDictionary<CosmosDbReference, Lazy<Task<CosmosClient>>> _clients = new();

    public Task<CosmosClient> GetCosmosClientAsync(string connectionString, string databaseId)
    {
        CosmosDbReference reference = new(connectionString, databaseId);

        Lazy<Task<CosmosClient>> lazy = _clients.GetOrAdd(reference, key =>
        {
            if (!configurator.DatabasesConfig.TryGetValue(key, out CosmosDbDatabaseOptions? options))
            {
                throw new InvalidOperationException(
                    $"No CosmosDB database configured for connection string '{connectionString}' / database '{databaseId}'.");
            }

            return new Lazy<Task<CosmosClient>>(() => CreateClientAsync(options));
        });

        return lazy.Value;
    }

    private static Task<CosmosClient> CreateClientAsync(CosmosDbDatabaseOptions options)
    {
        CosmosClientOptions clientOptions = new()
        {
            ConnectionMode = options.UseIntegratedCache ? ConnectionMode.Gateway : ConnectionMode.Direct,
            ConsistencyLevel = ConsistencyLevel.Session,
            CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions
            {
                DisableDistributedTracing = false
            }
        };

        if (options.IgnoreSslCertificateValidation)
        {
            clientOptions.HttpClientFactory = () =>
            {
                HttpMessageHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(handler);
            };
        }

        return Task.FromResult(new CosmosClient(options.ConnectionString, clientOptions));
    }
}
