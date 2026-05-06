using Microsoft.Extensions.Logging;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbMigrations;

namespace Users.Infrastructure.CosmosDb.Migrations;

internal sealed class MigrationService(
    IServiceProvider serviceProvider,
    IEnumerable<IMigration> migrations,
    IMigrationRepository migrationRepository,
    ILogger<MigrationService> logger)
    : IMigrationService
{
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var applied = await migrationRepository.GetAllAsync(cancellationToken);

        var pending = migrations
            .Where(m => applied.All(a => a.Version != m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("No pending migrations");
            return;
        }

        logger.LogInformation("{Count} migration(s) pending", pending.Count);

        foreach (var migration in pending)
        {
            logger.LogInformation("Applying migration {Version}", migration.Version);

            await migration.UpAsync(serviceProvider, cancellationToken);

            await migrationRepository.AddAsync(
                new DbMigration { Id = Guid.NewGuid().ToString(), Version = migration.Version },
                cancellationToken);

            logger.LogInformation("Migration {Version} applied", migration.Version);
        }
    }
}
