using Users.Infrastructure.Entities.Models.DbTenants;

namespace Users.Infrastructure.Contracts.Repositories;

public interface ITenantRepository : IRepository<DbTenant, string>
{
    // WARNING: cross-partition fan-out — returns tenants whose parentId matches
    Task<IReadOnlyList<DbTenant>> GetChildrenAsync(string parentId, CancellationToken cancellationToken);

    Task AddChildAsync(string parentId, string childId, CancellationToken cancellationToken);

    Task RemoveChildAsync(string parentId, string childId, CancellationToken cancellationToken);
}
