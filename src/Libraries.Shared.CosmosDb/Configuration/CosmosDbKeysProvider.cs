using Microsoft.Azure.Cosmos;

namespace Libraries.Shared.CosmosDb.Configuration;

internal sealed class CosmosDbKeysProvider<T>(CosmosDbConfigurator configurator) : ICosmosDbKeysProvider<T>
{
    public PartitionKey GetPartitionKey(T obj)
    {
        CosmosDbContainerOptions<T> options = GetContainerOptions();
        string pk = options.PartitionKeyGenerator!(obj);
        return new PartitionKey(pk);
    }

    public string GetPrimaryKey(T obj)
    {
        CosmosDbContainerOptions<T> options = GetContainerOptions();
        return options.PrimaryKeyGenerator!(obj);
    }

    private CosmosDbContainerOptions<T> GetContainerOptions()
    {
        foreach (CosmosDbDatabaseOptions db in configurator.DatabasesConfig.Values)
        {
            if (db.ContainerBuilder.ContainersConfig.TryGetValue(typeof(T), out ICosmosDbContainerOptions? opts))
            {
                return (CosmosDbContainerOptions<T>) opts;
            }
        }

        throw new InvalidOperationException($"No container configured for type '{typeof(T).FullName}'.");
    }
}
