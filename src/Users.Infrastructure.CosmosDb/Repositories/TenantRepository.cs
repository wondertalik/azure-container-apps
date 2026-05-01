using Libraries.Shared.Services;
using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbTenants;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class TenantRepository(
    ICosmosDbContainerProvider<DbTenant> containerProvider,
    ICosmosDbKeysProvider<DbTenant> keysProvider,
    ICurrentDateTimeService dateTimeService)
    : SoftDeleteCosmosRepository<DbTenant>(containerProvider, keysProvider, dateTimeService), ITenantRepository
{
    // IRepository.GetAsync(id, ct) — tenants use id as partition key
    Task<DbTenant?> IRepository<DbTenant, string>.GetAsync(string id, CancellationToken cancellationToken)
    {
        return GetAsync(id, id, cancellationToken);
    }

    // IRepository.DeleteAsync(id, deletedBy, ct) — tenants use id as partition key
    Task IRepository<DbTenant, string>.DeleteAsync(string id, Guid deletedBy, CancellationToken cancellationToken)
    {
        return DeleteAsync(id, id, deletedBy, cancellationToken);
    }

    public async Task<IReadOnlyList<DbTenant>> GetChildrenAsync(string parentId, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE c.parentId = @parentId AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))")
            .WithParameter("@parentId", parentId);
        return await ExecuteQueryAsync<DbTenant>(query, container, cancellationToken);
    }

    public async Task AddChildAsync(string parentId, string childId, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        await container.PatchItemAsync<DbTenant>(
            parentId,
            new PartitionKey(parentId),
            [PatchOperation.Add("/childIds/-", childId)],
            cancellationToken: cancellationToken);
    }

    public async Task RemoveChildAsync(string parentId, string childId, CancellationToken cancellationToken)
    {
        var parent = await GetAsync(parentId, parentId, cancellationToken);
        if (parent is null)
        {
            return;
        }

        var childList = parent.ChildIds.ToList();
        var idx = childList.IndexOf(childId);
        if (idx < 0)
        {
            return;
        }

        Container container = await ContainerProvider.GetContainerAsync();
        await container.PatchItemAsync<DbTenant>(
            parentId,
            new PartitionKey(parentId),
            [PatchOperation.Remove($"/childIds/{idx}")],
            cancellationToken: cancellationToken);
    }
}
