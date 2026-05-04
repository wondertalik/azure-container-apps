using Libraries.Shared.Services;
using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbUsers;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class UserRepository(
    ICosmosDbContainerProvider<DbUser> containerProvider,
    ICosmosDbKeysProvider<DbUser> keysProvider,
    ICurrentDateTimeService dateTimeService)
    : SoftDeleteCosmosRepository<DbUser>(containerProvider, keysProvider, dateTimeService), IUserRepository
{
    // IRepository.GetAsync(id, ct) — users use id as partition key
    Task<DbUser?> IRepository<DbUser, string>.GetAsync(string id, CancellationToken cancellationToken)
    {
        return GetAsync(id, id, cancellationToken);
    }

    // IRepository.DeleteAsync(id, deletedBy, ct) — users use id as partition key
    Task IRepository<DbUser, string>.DeleteAsync(string id, string deletedBy, CancellationToken cancellationToken)
    {
        return DeleteAsync(id, id, deletedBy, cancellationToken);
    }

    public async Task<DbUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        var query = new QueryDefinition(
                $"SELECT * FROM {container.Id} c WHERE c.email = @email AND (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt)) OFFSET 0 LIMIT 1")
            .WithParameter("@email", email);
        var results = await ExecuteQueryAsync<DbUser>(query, container, cancellationToken);
        return results.FirstOrDefault();
    }

    public async Task PatchAddTenantAssignmentAsync(
        string userId,
        DbUser.DbTenantAssignment assignment,
        CancellationToken cancellationToken)
    {
        Container container = await ContainerProvider.GetContainerAsync();
        await container.PatchItemAsync<DbUser>(
            userId,
            new PartitionKey(userId),
            [PatchOperation.Add("/tenantAssignments/-", assignment)],
            cancellationToken: cancellationToken);
    }

    public async Task PatchRemoveTenantAssignmentAsync(
        string userId,
        string tenantId,
        string deletedBy,
        CancellationToken cancellationToken)
    {
        var user = await GetAsync(userId, userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var match = user.TenantAssignments
            .Select((t, i) => (t, i))
            .FirstOrDefault(x => x.t.TenantId == tenantId && x.t.DeletedAt is null);

        if (match.t is null)
        {
            return;
        }

        Container container = await ContainerProvider.GetContainerAsync();
        await container.PatchItemAsync<DbUser>(
            userId,
            new PartitionKey(userId),
            [
                PatchOperation.Set($"/tenantAssignments/{match.i}/deletedAt", DateTimeService.UtcNow()),
                PatchOperation.Set($"/tenantAssignments/{match.i}/deletedBy", deletedBy)
            ],
            cancellationToken: cancellationToken);
    }
}
