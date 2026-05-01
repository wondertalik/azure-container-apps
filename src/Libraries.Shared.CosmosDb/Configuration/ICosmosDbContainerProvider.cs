namespace Libraries.Shared.CosmosDb.Configuration;

public interface ICosmosDbContainerProvider<T>
{
    Task<Microsoft.Azure.Cosmos.Container> GetContainerAsync();
}
