using Users.Infrastructure.Entities.Models.DbUsers;

namespace Users.Infrastructure.Contracts.Repositories;

public interface IUserRepository : IRepository<DbUser, string>
{
    Task<DbUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task PatchAddTenantAssignmentAsync(
        string userId,
        DbUser.DbTenantAssignment assignment,
        CancellationToken cancellationToken);

    Task PatchRemoveTenantAssignmentAsync(
        string userId,
        string tenantId,
        string deletedBy,
        CancellationToken cancellationToken);
}
