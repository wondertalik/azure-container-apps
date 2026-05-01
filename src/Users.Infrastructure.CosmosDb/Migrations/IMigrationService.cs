namespace Users.Infrastructure.CosmosDb.Migrations;

internal interface IMigrationService
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken);
}
