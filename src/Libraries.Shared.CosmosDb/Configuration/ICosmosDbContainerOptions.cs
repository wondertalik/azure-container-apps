namespace Libraries.Shared.CosmosDb.Configuration;

public interface ICosmosDbContainerOptions
{
    string Name { get; }

    string PartitionKey { get; }
}
