namespace Libraries.Shared.CosmosDb.Configuration;

public sealed class CosmosDbContainerOptions<T> : ICosmosDbContainerOptions
{
    public string Name { get; private set; } = string.Empty;

    public string PartitionKey { get; private set; } = string.Empty;

    public Func<T, string>? PrimaryKeyGenerator { get; private set; }

    public Func<T, string>? PartitionKeyGenerator { get; private set; }

    public CosmosDbContainerOptions<T> WithName(string name)
    {
        Name = name;
        return this;
    }

    public CosmosDbContainerOptions<T> WithPartitionKeyPath(string partitionKeyPath)
    {
        PartitionKey = partitionKeyPath;
        return this;
    }

    public CosmosDbContainerOptions<T> WithPrimaryKey(Func<T, string> primaryKeyGenerator)
    {
        PrimaryKeyGenerator = primaryKeyGenerator;
        return this;
    }

    public CosmosDbContainerOptions<T> WithPartitionKey(Func<T, string> partitionKeyGenerator)
    {
        PartitionKeyGenerator = partitionKeyGenerator;
        return this;
    }
}
