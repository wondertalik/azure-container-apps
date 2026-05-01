using Users.Infrastructure.Entities.Models.DbRoles;

namespace Users.Infrastructure.Contracts.Repositories;

public interface IRoleRepository
{
    Task<DbRole?> GetByIdAsync(string roleId, string tenantId, CancellationToken cancellationToken);

    // WARNING: cross-partition fan-out
    Task<IReadOnlyList<DbRole>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DbRole>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbRole>> GetGlobalRolesAsync(CancellationToken cancellationToken);

    Task<DbRole> AddAsync(DbRole role, CancellationToken cancellationToken);

    Task<DbRole> UpdateAsync(DbRole role, CancellationToken cancellationToken);

    Task DeleteAsync(string roleId, string tenantId, CancellationToken cancellationToken);
}
