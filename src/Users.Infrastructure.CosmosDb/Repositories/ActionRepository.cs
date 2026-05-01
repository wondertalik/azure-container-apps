using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbActions;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class ActionRepository(
    ICosmosDbContainerProvider<DbAction> containerProvider,
    ICosmosDbKeysProvider<DbAction> keysProvider)
    : IActionRepository
{
    public async Task<DbAction?> GetAsync(string actionId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        try
        {
            var response = await container.ReadItemAsync<DbAction>(actionId, new PartitionKey(actionId),
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DbAction>> GetAllAsync(CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition($"SELECT * FROM {container.Id} c");
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<DbAction>> GetByIdsAsync(
        IReadOnlyList<string> actionIds,
        CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE ARRAY_CONTAINS(@ids, c.id)")
            .WithParameter("@ids", actionIds.ToArray());
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<DbAction> AddAsync(DbAction action, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var response = await container.CreateItemAsync(action, keysProvider.GetPartitionKey(action),
            cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string actionId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        await container.DeleteItemAsync<DbAction>(actionId, new PartitionKey(actionId),
            cancellationToken: cancellationToken);
    }

    private static async Task<List<DbAction>> ExecuteQueryAsync(
        QueryDefinition query,
        Container container,
        CancellationToken cancellationToken)
    {
        var result = new List<DbAction>();
        using var iterator = container.GetItemQueryIterator<DbAction>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            result.AddRange(page);
        }

        return result;
    }
}
