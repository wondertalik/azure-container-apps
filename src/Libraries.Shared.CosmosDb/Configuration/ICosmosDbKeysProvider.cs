using Microsoft.Azure.Cosmos;

namespace Libraries.Shared.CosmosDb.Configuration;

public interface ICosmosDbKeysProvider<T>
{
    PartitionKey GetPartitionKey(T obj);
    string GetPrimaryKey(T obj);
}
