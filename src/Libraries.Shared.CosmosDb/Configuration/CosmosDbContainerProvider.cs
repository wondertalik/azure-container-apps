using Microsoft.Azure.Cosmos;

namespace Libraries.Shared.CosmosDb.Configuration;

internal sealed class CosmosDbContainerProvider<T> : ICosmosDbContainerProvider<T>
{
    private readonly Lazy<Task<Container>> _lazyContainer;

    public CosmosDbContainerProvider(CosmosDbClientProvider clientProvider, CosmosDbConfigurator configurator)
    {
        _lazyContainer = new Lazy<Task<Container>>(() => ResolveContainerAsync(clientProvider, configurator));
    }

    public Task<Container> GetContainerAsync()
    {
        return _lazyContainer.Value;
    }

    private static async Task<Container> ResolveContainerAsync(
        CosmosDbClientProvider clientProvider,
        CosmosDbConfigurator configurator)
    {
        foreach (KeyValuePair<CosmosDbReference, CosmosDbDatabaseOptions> entry in configurator.DatabasesConfig)
        {
            if (entry.Value.ContainerBuilder.ContainersConfig.TryGetValue(typeof(T),
                    out ICosmosDbContainerOptions? opts))
            {
                CosmosClient client = await clientProvider.GetCosmosClientAsync(
                    entry.Key.ConnectionString, entry.Key.DatabaseId);

                return client.GetDatabase(entry.Key.DatabaseId).GetContainer(opts.Name);
            }
        }

        throw new InvalidOperationException($"No container configured for type '{typeof(T).FullName}'.");
    }
}
