using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.CosmosDb.Migrations;

namespace Users.Infrastructure.CosmosDb.Extensions;

public static class HostExtensions
{
    public static async Task<IHost> UseUsersCosmosDbAsync(
        this IHost host, CancellationToken cancellationToken = default)
    {
        host.UseUsersCosmosDb();

        var manager = host.Services.GetRequiredService<IUsersCosmosDbManagerRepository>();
        await manager.CreateDatabaseIfNotExistsAsync(cancellationToken);

        return host;
    }

    public static async Task<IHost> ApplyUsersMigrationsAsync(
        this IHost host, CancellationToken cancellationToken = default)
    {
        var migrationService = host.Services.GetRequiredService<IMigrationService>();
        await migrationService.ApplyMigrationsAsync(cancellationToken);
        return host;
    }
}
