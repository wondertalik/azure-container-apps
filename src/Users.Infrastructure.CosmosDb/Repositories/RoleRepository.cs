using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Authorization.Constants;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbRoles;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class RoleRepository(
    ICosmosDbContainerProvider<DbRole> containerProvider,
    ICosmosDbKeysProvider<DbRole> keysProvider)
    : IRoleRepository
{
    public async Task<DbRole?> GetByIdAsync(string roleId, string tenantId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        try
        {
            var response = await container.ReadItemAsync<DbRole>(roleId, new PartitionKey(tenantId),
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DbRole>> GetAllAsync(CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition($"SELECT * FROM {container.Id} c");
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<DbRole>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition($"SELECT * FROM {container.Id} c WHERE c.tenantId = @tenantId")
            .WithParameter("@tenantId", tenantId);
        return await ExecuteQueryAsync(query, container, cancellationToken);
    }

    public async Task<IReadOnlyList<DbRole>> GetGlobalRolesAsync(CancellationToken cancellationToken)
    {
        return await GetByTenantIdAsync(Root.SystemId, cancellationToken);
    }

    public async Task<DbRole> AddAsync(DbRole role, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var pk = keysProvider.GetPartitionKey(role);
        var response = await container.CreateItemAsync(role, pk, cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<DbRole> UpdateAsync(DbRole role, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var pk = keysProvider.GetPartitionKey(role);
        var response = await container.ReplaceItemAsync(role, role.RoleId, pk, cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string roleId, string tenantId, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        await container.DeleteItemAsync<DbRole>(roleId, new PartitionKey(tenantId),
            cancellationToken: cancellationToken);
    }

    private static async Task<List<DbRole>> ExecuteQueryAsync(
        QueryDefinition query,
        Container container,
        CancellationToken cancellationToken)
    {
        var result = new List<DbRole>();
        using var iterator = container.GetItemQueryIterator<DbRole>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            result.AddRange(page);
        }

        return result;
    }
}
