using Microsoft.Extensions.DependencyInjection;
using Users.Infrastructure.CosmosDb.Migrations;

namespace Users.Infrastructure.CosmosDb.Migrations;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersCosmosDbMigrations(this IServiceCollection services)
    {
        services.AddSingleton<IMigration, V20250501_202100_InitialSeed>();

        return services;
    }
}
