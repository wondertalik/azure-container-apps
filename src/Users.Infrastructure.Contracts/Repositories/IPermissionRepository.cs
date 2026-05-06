using Users.Infrastructure.Entities.Models.DbPermissions;

namespace Users.Infrastructure.Contracts.Repositories;

public interface IPermissionRepository
{
    Task<DbPermission?> GetAsync(string userId, string tenantId, CancellationToken cancellationToken);

    // WARNING: cross-partition fan-out
    Task<IReadOnlyList<DbPermission>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbPermission>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken);

    Task<DbPermission> AddAsync(DbPermission permission, CancellationToken cancellationToken);

    Task<DbPermission> UpdateAsync(DbPermission permission, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, string tenantId, string deletedBy, CancellationToken cancellationToken);
}
