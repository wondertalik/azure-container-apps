namespace Libraries.Shared.CosmosDb.Configuration;

public sealed class CosmosDbConfigurator
{
    private readonly Dictionary<CosmosDbReference, CosmosDbDatabaseOptions> _databasesConfig = new();

    public IReadOnlyDictionary<CosmosDbReference, CosmosDbDatabaseOptions> DatabasesConfig => _databasesConfig;

    public void ConfigureDatabase(Action<CosmosDbDatabaseOptions> configure)
    {
        CosmosDbDatabaseOptions databaseOptions = new();
        configure(databaseOptions);

        CosmosDbReference reference = new(databaseOptions.ConnectionString, databaseOptions.DatabaseId);

        if (!_databasesConfig.ContainsKey(reference))
        {
            _databasesConfig.Add(reference, databaseOptions);
        }
        else
        {
            _databasesConfig[reference].ContainerBuilder.Merge(databaseOptions.ContainerBuilder);
        }
    }
}
