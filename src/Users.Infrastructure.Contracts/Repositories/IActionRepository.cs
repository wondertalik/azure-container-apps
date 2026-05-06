using Users.Infrastructure.Entities.Models.DbActions;

namespace Users.Infrastructure.Contracts.Repositories;

public interface IActionRepository
{
    Task<DbAction?> GetAsync(string actionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbAction>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DbAction>> GetByIdsAsync(
        IReadOnlyList<string> actionIds,
        CancellationToken cancellationToken);

    Task<DbAction> AddAsync(DbAction action, CancellationToken cancellationToken);

    Task DeleteAsync(string actionId, CancellationToken cancellationToken);
}
