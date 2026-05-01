using Libraries.Shared.Services;
using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbPermissions;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class PermissionRepository(
    ICosmosDbContainerProvider<DbPermission> containerProvider,
    ICosmosDbKeysProvider<DbPermission> keysProvider,
    ICurrentDateTimeService dateTimeService)
    : IPermissionRepository
{
    public async Task<DbPermission?> GetAsync(string userId, string tenantId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        try
        {
            var response = await container.ReadItemAsync<DbPermission>(userId, new PartitionKey(tenantId),
                cancellationToken: cancellationToken);
            return response.Resource?.DeletedAt is null ? response.Resource : null;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DbPermission>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE c.id = @userId AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))")
            .WithParameter("@userId", userId);
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<DbPermission>> GetByTenantIdAsync(string tenantId,
        CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE c.tenantId = @tenantId AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))")
            .WithParameter("@tenantId", tenantId);
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<DbPermission> AddAsync(DbPermission permission, CancellationToken cancellationToken)
    {
        permission.DeletedAt = null;
        permission.DeletedBy = null;
        Container container = await containerProvider.GetContainerAsync();
        var response = await container.CreateItemAsync(permission, keysProvider.GetPartitionKey(permission),
            cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<DbPermission> UpdateAsync(DbPermission permission, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var response = await container.ReplaceItemAsync(permission, permission.UserId,
            keysProvider.GetPartitionKey(permission),
            cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string userId, string tenantId, Guid deletedBy, CancellationToken cancellationToken)
    {
        var permission = await GetAsync(userId, tenantId, cancellationToken);
        if (permission is null)
        {
            return;
        }

        permission.DeletedAt = dateTimeService.UtcNow();
        permission.DeletedBy = deletedBy;
        await UpdateAsync(permission, cancellationToken);
    }

    private static async Task<List<DbPermission>> ExecuteQueryAsync(
        QueryDefinition query,
        Container container,
        CancellationToken cancellationToken)
    {
        var result = new List<DbPermission>();
        using var iterator = container.GetItemQueryIterator<DbPermission>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            result.AddRange(page);
        }

        return result;
    }
}
