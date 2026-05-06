using Libraries.Shared.CosmosDb;
using Libraries.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Infrastructure.CosmosDb.Migrations;
using Users.Infrastructure.CosmosDb.Options;
using Users.Infrastructure.CosmosDb.Repositories;
using Users.Infrastructure.Contracts.Repositories;

namespace Users.Infrastructure.CosmosDb;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersCosmosDb(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCosmosDb();
        services.AddOptionsAndValidateOnStart<UsersInfrastructureCosmosDbOptions>(
            configuration, UsersInfrastructureCosmosDbOptions.ConfigSectionName);

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ITenantRepository, TenantRepository>();
        services.AddSingleton<IRoleRepository, RoleRepository>();
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<IPermissionRepository, PermissionRepository>();
        services.AddSingleton<IMigrationRepository, MigrationRepository>();

        services.AddSingleton<IUsersCosmosDbManagerRepository, UsersCosmosDbManagerRepository>();
        services.AddSingleton<IMigrationService, MigrationService>();

        return services;
    }
}
