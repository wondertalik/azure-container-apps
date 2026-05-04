using System.Net;
using Libraries.Shared.Interfaces;
using Libraries.Shared.Services;
using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;

namespace Users.Infrastructure.CosmosDb.Repositories;

public abstract class SoftDeleteCosmosRepository<T>(
    ICosmosDbContainerProvider<T> containerProvider,
    ICosmosDbKeysProvider<T> keysProvider,
    ICurrentDateTimeService dateTimeService)
    where T : class, ISoftDeletable
{
    protected readonly ICosmosDbContainerProvider<T> ContainerProvider = containerProvider;

    protected readonly ICosmosDbKeysProvider<T> KeysProvider = keysProvider;

    protected readonly ICurrentDateTimeService DateTimeService = dateTimeService;

    public sealed record RecordsAffected(string PartitionKey, int Count);

    public sealed record RecordExists(string Id, bool Exists);

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
    {
        entity.DeletedAt = null;
        entity.DeletedBy = null;

        Container container = await ContainerProvider.GetContainerAsync();
        PartitionKey pk = KeysProvider.GetPartitionKey(entity);
        var response = await container.CreateItemAsync(entity, pk, cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<IReadOnlyList<RecordsAffected>> AddMultipleAsync(
        IReadOnlyList<T> entities,
        CancellationToken cancellationToken)
    {
        foreach (var e in entities)
        {
            e.DeletedAt = null;
            e.DeletedBy = null;
        }

        return await ExecuteTransactionsAsync(entities, (batch, item) => batch.CreateItem(item), cancellationToken);
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        string id = KeysProvider.GetPrimaryKey(entity);
        PartitionKey pk = KeysProvider.GetPartitionKey(entity);
        var response = await container.ReplaceItemAsync(entity, id, pk, cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<IReadOnlyList<RecordsAffected>> UpdateMultipleAsync(
        IReadOnlyList<T> entities,
        CancellationToken cancellationToken)
    {
        return await ExecuteTransactionsAsync(entities,
            (batch, item) => batch.ReplaceItem(KeysProvider.GetPrimaryKey(item), item),
            cancellationToken);
    }

    public async Task<T?> GetAsync(string id, string partitionKey, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        using var response = await container.ReadItemStreamAsync(id, new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var serializer = container.Database.Client.ClientOptions.Serializer;
        var doc = serializer.FromStream<T>(response.Content);
        return doc?.DeletedAt is null ? doc : null;
    }

    public async Task<T?> GetIncludingDeletedAsync(string id, string partitionKey, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        using var response = await container.ReadItemStreamAsync(id, new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var serializer = container.Database.Client.ClientOptions.Serializer;
        return serializer.FromStream<T>(response.Content);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var query = new QueryDefinition(
            $"SELECT * FROM {container.Id} c WHERE (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))");
        return await ExecuteQueryAsync<T>(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetMultipleAsync(string id, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE c.id = @id AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))")
            .WithParameter("@id", id);
        return await ExecuteQueryAsync<T>(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetMultipleAsync(
        IReadOnlyList<(string id, string partitionKey)> ids,
        CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var list = ids.Select(x => (x.id, new PartitionKey(x.partitionKey))).ToList();
        FeedResponse<T> response = await container.ReadManyItemsAsync<T>(list, cancellationToken: cancellationToken);
        return response.Where(x => x.DeletedAt is null).ToList();
    }

    public async Task<IReadOnlyList<RecordExists>> ExistsAsync(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT DISTINCT VALUE c.id FROM {container.Id} c WHERE ARRAY_CONTAINS(@ids, c.id) AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))")
            .WithParameter("@ids", ids.ToArray());

        var found = await ExecuteQueryAsync<string>(query, container, cancellationToken);
        return ids.Select(id => new RecordExists(id, found.Contains(id))).ToList();
    }

    public async Task DeleteAsync(string id, string partitionKey, string deletedBy, CancellationToken cancellationToken)
    {
        var entity = await GetAsync(id, partitionKey, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.DeletedAt = DateTimeService.UtcNow();
        entity.DeletedBy = deletedBy;
        await UpdateAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<RecordsAffected>> DeleteMultipleAsync(
        IReadOnlyList<T> entities,
        string deletedBy,
        CancellationToken cancellationToken)
    {
        foreach (var e in entities)
        {
            e.DeletedAt = DateTimeService.UtcNow();
            e.DeletedBy = deletedBy;
        }

        return await UpdateMultipleAsync(entities, cancellationToken);
    }

    protected static async Task<List<TValue>> ExecuteQueryAsync<TValue>(
        QueryDefinition query,
        Container container,
        CancellationToken cancellationToken)
    {
        var result = new List<TValue>();
        using var iterator = container.GetItemQueryIterator<TValue>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            result.AddRange(page);
        }

        return result;
    }

    private async Task<IReadOnlyList<RecordsAffected>> ExecuteTransactionsAsync(
        IReadOnlyList<T> entities,
        Action<TransactionalBatch, T> action,
        CancellationToken cancellationToken)
    {
        var partitioned = entities.GroupBy(KeysProvider.GetPartitionKey).ToList();
        var affected = new List<RecordsAffected>();

        foreach (var partition in partitioned)
        {
            Container container = await ContainerProvider.GetContainerAsync();
            var batch = container.CreateTransactionalBatch(partition.Key);
            foreach (var item in partition)
            {
                action(batch, item);
            }

            var response = await batch.ExecuteAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK && response.Count > 0)
            {
                affected.Add(new RecordsAffected(partition.Key.ToString(), response.Count));
            }
        }

        return affected;
    }
}
