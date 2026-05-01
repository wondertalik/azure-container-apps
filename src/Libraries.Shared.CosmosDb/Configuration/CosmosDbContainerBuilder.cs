namespace Libraries.Shared.CosmosDb.Configuration;

public sealed class CosmosDbContainerBuilder
{
    private readonly Dictionary<Type, ICosmosDbContainerOptions> _containersConfig = new();

    public IReadOnlyDictionary<Type, ICosmosDbContainerOptions> ContainersConfig => _containersConfig;

    public CosmosDbContainerBuilder Configure<T>(Action<CosmosDbContainerOptions<T>> configure)
    {
        CosmosDbContainerOptions<T> options = new();
        configure(options);
        _containersConfig[typeof(T)] = options;
        return this;
    }

    internal void Merge(CosmosDbContainerBuilder other)
    {
        foreach (KeyValuePair<Type, ICosmosDbContainerOptions> entry in other.ContainersConfig)
        {
            _containersConfig[entry.Key] = entry.Value;
        }
    }
}
