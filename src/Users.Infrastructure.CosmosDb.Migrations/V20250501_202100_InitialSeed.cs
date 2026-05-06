using Microsoft.Extensions.DependencyInjection;
using Users.Authorization.Constants;
using Users.Authorization.Constants.Actions;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbActions;
using Users.Infrastructure.Entities.Models.DbPermissions;
using Users.Infrastructure.Entities.Models.DbRoles;
using Users.Infrastructure.Entities.Models.DbTenants;
using Users.Infrastructure.Entities.Models.DbUsers;
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
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var permissionRepository = serviceProvider.GetRequiredService<IPermissionRepository>();

        var now = DateTimeOffset.UtcNow;

        await SeedActionsAsync(actionRepository, cancellationToken);
        await SeedRolesAsync(roleRepository, cancellationToken);
        await SeedRootTenantAsync(tenantRepository, now, cancellationToken);
        await SeedSystemUserAsync(userRepository, permissionRepository, now, cancellationToken);
    }

    private static async Task SeedActionsAsync(
        IActionRepository actionRepository,
        CancellationToken cancellationToken)
    {
        var actions = new[]
        {
            (TenantActions.TenantsView, "View Galactic Orders"),
            (TenantActions.ModuleTenants, "Access Galactic Orders Registry"),
            (TenantActions.TenantsAdd, "Establish New Order"),
            (TenantActions.TenantsEdit, "Reorganize Order Structure"),
            (TenantActions.TenantsDelete, "Dissolve Galactic Order"),
            (UserActions.ModuleUsers, "Access Force Registry"),
            (UserActions.UsersView, "Sense Members of Own Order"),
            (UserActions.UsersViewAllTenants, "Sense All Force Beings Across the Galaxy"),
            (UserActions.UsersAdd, "Recruit to Own Order"),
            (UserActions.UsersAddAllTenants, "Recruit Across All Orders"),
            (UserActions.UsersEdit, "Modify Record in Own Order"),
            (UserActions.UsersEditAllTenants, "Modify Records Across All Orders"),
            (UserActions.UsersAssignExplicitActions, "Grant Force Abilities Directly"),
            (UserActions.UsersAssignRoles, "Bestow Force Rank"),
            (UserActions.UsersDelete, "Exile from Own Order"),
            (UserActions.UsersDeleteAllTenants, "Exile from the Galaxy"),
            (UserActions.UsersEditOwnProfile, "Update Own Holocron"),
            (AuthActions.AuthGetRoles, "Consult Rank Hierarchy of Own Order"),
            (AuthActions.AuthGetRolesAllTenants, "Consult All Rank Hierarchies"),
            (AuthActions.AuthGetActions, "Access the Jedi Archives")
        };

        foreach ((string actionId, string name) in actions)
        {
            var existing = await actionRepository.GetAsync(actionId, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await actionRepository.AddAsync(new DbAction { ActionId = actionId, Name = name }, cancellationToken);
        }
    }

    private static async Task SeedRolesAsync(
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new DbRole
            {
                RoleId = Roles.ChosenOneId,
                Name = "The Chosen One",
                TenantId = Root.TenantId,
                ActionIds =
                [
                    TenantActions.TenantsView,
                    TenantActions.ModuleTenants,
                    TenantActions.TenantsAdd,
                    TenantActions.TenantsEdit,
                    TenantActions.TenantsDelete,
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
            new DbRole
            {
                RoleId = Roles.CouncilMemberId,
                Name = "Council Member",
                TenantId = Root.SystemId,
                ActionIds =
                [
                    TenantActions.TenantsView,
                    TenantActions.ModuleTenants,
                    TenantActions.TenantsAdd,
                    TenantActions.TenantsEdit,
                    UserActions.ModuleUsers,
                    UserActions.UsersViewAllTenants,
                    UserActions.UsersAddAllTenants,
                    UserActions.UsersEditAllTenants,
                    UserActions.UsersAssignRoles,
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRolesAllTenants,
                    AuthActions.AuthGetActions
                ]
            },
            new DbRole
            {
                RoleId = Roles.KnightCommanderId,
                Name = "Knight Commander",
                TenantId = Root.SystemId,
                ActionIds =
                [
                    TenantActions.TenantsView,
                    TenantActions.ModuleTenants,
                    UserActions.ModuleUsers,
                    UserActions.UsersView,
                    UserActions.UsersAdd,
                    UserActions.UsersEdit,
                    UserActions.UsersDelete,
                    UserActions.UsersAssignRoles,
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRoles,
                    AuthActions.AuthGetActions
                ]
            },
            new DbRole
            {
                RoleId = Roles.KnightId,
                Name = "Knight",
                TenantId = Root.SystemId,
                ActionIds =
                [
                    TenantActions.TenantsView,
                    UserActions.ModuleUsers,
                    UserActions.UsersView,
                    UserActions.UsersEdit,
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRoles,
                    AuthActions.AuthGetActions
                ]
            },
            new DbRole
            {
                RoleId = Roles.PadawanId,
                Name = "Padawan",
                TenantId = Root.SystemId,
                ActionIds =
                [
                    UserActions.ModuleUsers,
                    UserActions.UsersView,
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRoles
                ]
            },
            new DbRole
            {
                RoleId = Roles.YounglingId,
                Name = "Youngling",
                TenantId = Root.SystemId,
                ActionIds =
                [
                    UserActions.UsersEditOwnProfile,
                    AuthActions.AuthGetRoles
                ]
            },
            new DbRole
            {
                RoleId = Roles.OperatorId,
                Name = "The Operator",
                TenantId = Root.TenantId,
                ActionIds =
                [
                    TenantActions.TenantsView,
                    TenantActions.ModuleTenants,
                    TenantActions.TenantsAdd,
                    TenantActions.TenantsEdit,
                    TenantActions.TenantsDelete,
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
            }
        };

        foreach (var role in roles)
        {
            var existing = await roleRepository.GetByIdAsync(role.RoleId, role.TenantId, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await roleRepository.AddAsync(role, cancellationToken);
        }
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
                Name = "The Force",
                ParentId = Root.SystemId,
                TenantType = TenantType.Node,
                CreatedAt = now,
                CreatedBy = Root.SystemId,
                UpdatedAt = now,
                UpdatedBy = Root.SystemId
            },
            cancellationToken);
    }

    private static async Task SeedSystemUserAsync(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var systemUserId = Root.SystemId;

        var existingUser = await userRepository.GetAsync(systemUserId, cancellationToken);
        if (existingUser is null)
        {
            await userRepository.AddAsync(
                new DbUser
                {
                    UserId = systemUserId,
                    Firstname = "Jawa",
                    Lastname = "Utini",
                    Email = "jawa@the-force.sw",
                    AccountEnabled = true,
                    DefaultTenantId = Root.TenantId,
                    TenantAssignments =
                    [
                        new DbUser.DbTenantAssignment
                        {
                            TenantId = Root.TenantId,
                            CreatedAt = now,
                            CreatedBy = Root.SystemId
                        }
                    ],
                    CreatedAt = now,
                    CreatedBy = Root.SystemId,
                    UpdatedAt = now,
                    UpdatedBy = Root.SystemId
                },
                cancellationToken);
        }

        var existingPermission = await permissionRepository.GetAsync(systemUserId, Root.TenantId, cancellationToken);
        if (existingPermission is null)
        {
            await permissionRepository.AddAsync(
                new DbPermission
                {
                    UserId = systemUserId,
                    TenantId = Root.TenantId,
                    RoleAssignments =
                    [
                        new DbRoleAssignment { RoleId = Roles.OperatorId }
                    ],
                    CreatedAt = now,
                    CreatedBy = Root.SystemId,
                    UpdatedAt = now,
                    UpdatedBy = Root.SystemId
                },
                cancellationToken);
        }
    }
}
