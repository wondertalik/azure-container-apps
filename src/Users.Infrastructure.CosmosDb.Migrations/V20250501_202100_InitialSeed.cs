using Microsoft.Extensions.DependencyInjection;
using Users.Authorization.Constants;
using Users.Authorization.Constants.Actions;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbActions;
using Users.Infrastructure.Entities.Models.DbRoles;
using Users.Infrastructure.Entities.Models.DbTenants;
using Users.Shared.Enums;

namespace Users.Infrastructure.CosmosDb.Migrations;

internal sealed class V20250501_202100_InitialSeed : IMigration
{
    public string Version => "20250501_202100_InitialSeed";

    public async Task UpAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var actionRepository = serviceProvider.GetRequiredService<IActionRepository>();
        var roleRepository = serviceProvider.GetRequiredService<IRoleRepository>();
        var tenantRepository = serviceProvider.GetRequiredService<ITenantRepository>();

        var now = DateTimeOffset.UtcNow;

        await SeedActionsAsync(actionRepository, cancellationToken);
        await SeedRolesAsync(roleRepository, cancellationToken);
        await SeedRootTenantAsync(tenantRepository, now, cancellationToken);
    }

    private static async Task SeedActionsAsync(
        IActionRepository actionRepository,
        CancellationToken cancellationToken)
    {
        var actions = new[]
        {
            (TenantActions.TenantsView, "View tenant data"),
            (UserActions.ModuleUsers, "Access Users Section"),
            (UserActions.UsersView, "View users in own tenant"),
            (UserActions.UsersViewAllTenants, "View users in all tenants"),
            (UserActions.UsersAdd, "Add user to own tenant"),
            (UserActions.UsersAddAllTenants, "Add user to any tenant"),
            (UserActions.UsersEdit, "Edit user in own tenant"),
            (UserActions.UsersEditAllTenants, "Edit user in any tenant"),
            (UserActions.UsersAssignExplicitActions, "Assign explicit actions to user"),
            (UserActions.UsersAssignRoles, "Assign roles to user"),
            (UserActions.UsersDelete, "Delete user in own tenant"),
            (UserActions.UsersDeleteAllTenants, "Delete user in any tenant"),
            (UserActions.UsersEditOwnProfile, "Edit own profile"),
            (AuthActions.AuthGetRoles, "Get list of all roles in own tenant"),
            (AuthActions.AuthGetRolesAllTenants, "Get list of roles in any tenant"),
            (AuthActions.AuthGetActions, "Get list of all actions in the system")
        };

        foreach (var (actionId, name) in actions)
        {
            var existing = await actionRepository.GetAsync(actionId, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await actionRepository.AddAsync(
                new DbAction { ActionId = actionId, Name = name },
                cancellationToken);
        }
    }

    private static async Task SeedRolesAsync(
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var existing = await roleRepository.GetByIdAsync(
            Root.SuperAdminRoleId, Root.SystemId.ToString(), cancellationToken);

        if (existing is not null)
        {
            return;
        }

        await roleRepository.AddAsync(
            new DbRole
            {
                RoleId = Root.SuperAdminRoleId,
                Name = "Super Admin",
                TenantId = Root.SystemId.ToString(),
                ActionIds =
                [
                    TenantActions.TenantsView,
                    UserActions.ModuleUsers,
                    UserActions.UsersView,
                    UserActions.UsersViewAllTenants,
                    UserActions.UsersAdd,
                    UserActions.UsersAddAllTenants,
                    UserActions.UsersEdit,
                    UserActions.UsersEditAllTenants,
                    UserActions.UsersAssignExplicitActions,
                    UserActions.UsersAssignRoles,
                    UserActions.UsersDelete,
                    UserActions.UsersDeleteAllTenants,
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRoles,
                    AuthActions.AuthGetRolesAllTenants,
                    AuthActions.AuthGetActions
                ]
            },
            cancellationToken);
    }

    private static async Task SeedRootTenantAsync(
        ITenantRepository tenantRepository,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await tenantRepository.GetAsync(Root.TenantId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await tenantRepository.AddAsync(
            new DbTenant
            {
                TenantId = Root.TenantId,
                Name = "Root",
                ParentId = Root.SystemId.ToString(),
                TenantType = TenantType.Node,
                CreatedAt = now,
                CreatedBy = Root.SystemId,
                UpdatedAt = now,
                UpdatedBy = Root.SystemId
            },
            cancellationToken);
    }
}
