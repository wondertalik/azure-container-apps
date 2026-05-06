using Libraries.Shared.CosmosDb.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Users.Authorization.Constants;
using Users.Infrastructure.CosmosDb.Options;
using Users.Infrastructure.Entities.Models.DbActions;
using Users.Infrastructure.Entities.Models.DbMigrations;
using Users.Infrastructure.Entities.Models.DbPermissions;
using Users.Infrastructure.Entities.Models.DbRoles;
using Users.Infrastructure.Entities.Models.DbTenants;
using Users.Infrastructure.Entities.Models.DbUsers;

namespace Users.Infrastructure.CosmosDb;

public static class CosmosDbExtensions
{
    public static IHost UseUsersCosmosDb(this IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        CosmosDbConfigurator configurator = host.Services.GetRequiredService<CosmosDbConfigurator>();
        UsersInfrastructureCosmosDbOptions options =
            host.Services.GetRequiredService<IOptions<UsersInfrastructureCosmosDbOptions>>().Value;

        ConfigureUsersCosmosDbContainers(configurator, options);

        return host;
    }

    public static void ConfigureUsersCosmosDb(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        CosmosDbConfigurator configurator = serviceProvider.GetRequiredService<CosmosDbConfigurator>();
        UsersInfrastructureCosmosDbOptions options =
            serviceProvider.GetRequiredService<IOptions<UsersInfrastructureCosmosDbOptions>>().Value;

        ConfigureUsersCosmosDbContainers(configurator, options);
    }

    private static void ConfigureUsersCosmosDbContainers(CosmosDbConfigurator configurator,
        UsersInfrastructureCosmosDbOptions options)
    {
        configurator.ConfigureDatabase(db =>
        {
            db.ConnectionString = options.ConnectionString;
            db.DatabaseId = options.DatabaseId;
            db.Throughput = options.Throughput;
            db.UseIntegratedCache = options.UseIntegratedCache;
            db.IgnoreSslCertificateValidation = options.IgnoreSslCertificateValidation;

            db.ContainerBuilder
                .Configure<DbUser>(o => o
                    .WithName(UsersCosmosDbConstants.Users.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Users.PartitionKey)
                    .WithPrimaryKey(u => u.UserId)
                    .WithPartitionKey(u => u.UserId))
                .Configure<DbTenant>(o => o
                    .WithName(UsersCosmosDbConstants.Tenants.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Tenants.PartitionKey)
                    .WithPrimaryKey(t => t.TenantId)
                    .WithPartitionKey(t => t.TenantId))
                .Configure<DbRole>(o => o
                    .WithName(UsersCosmosDbConstants.Roles.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Roles.PartitionKey)
                    .WithPrimaryKey(r => r.RoleId)
                    .WithPartitionKey(r => r.TenantId))
                .Configure<DbAction>(o => o
                    .WithName(UsersCosmosDbConstants.Actions.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Actions.PartitionKey)
                    .WithPrimaryKey(a => a.ActionId)
                    .WithPartitionKey(a => a.ActionId))
                .Configure<DbPermission>(o => o
                    .WithName(UsersCosmosDbConstants.Permissions.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Permissions.PartitionKey)
                    .WithPrimaryKey(p => p.UserId)
                    .WithPartitionKey(p => p.TenantId))
                .Configure<DbMigration>(o => o
                    .WithName(UsersCosmosDbConstants.Migrations.Name)
                    .WithPartitionKeyPath(UsersCosmosDbConstants.Migrations.PartitionKey)
                    .WithPrimaryKey(m => m.Id)
                    .WithPartitionKey(m => m.Id));
        });
    }
}
