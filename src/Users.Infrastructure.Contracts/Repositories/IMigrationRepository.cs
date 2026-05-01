using Users.Infrastructure.Entities.Models.DbMigrations;

namespace Users.Infrastructure.Contracts.Repositories;

public interface IMigrationRepository
{
    Task<IReadOnlyList<DbMigration>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(DbMigration migration, CancellationToken cancellationToken);
}
