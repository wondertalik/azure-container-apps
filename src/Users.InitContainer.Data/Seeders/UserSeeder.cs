using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbPermissions;
using Users.Infrastructure.Entities.Models.DbUsers;
using Users.InitContainer.Data.Models;
using Users.InitContainer.Data.Options;

namespace Users.InitContainer.Data.Seeders;

public sealed class UserSeeder(
    IUserRepository userRepository,
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository,
    ITenantRepository tenantRepository,
    IOptions<SeederOptions> seederOptions,
    ILogger<UserSeeder> logger)
{
    private readonly SeederOptions _options = seederOptions.Value;

    public async Task SeedIfEnabledAsync(CancellationToken cancellationToken)
    {
        if (!_options.UsersSeed)
        {
            logger.LogInformation("User seeding is disabled via SeederOptions.UsersSeed");
            return;
        }

        logger.LogInformation(
            "Starting user seeding from {SeedDataFilePath}/users-db/users/",
            _options.SeedDataFilePath);

        await SeedFromJsonAsync(cancellationToken);

        logger.LogInformation("User seeding completed successfully");
    }

    private async Task SeedFromJsonAsync(CancellationToken cancellationToken)
    {
        var usersDirectory = Path.Combine(_options.SeedDataFilePath, "users-db", "users");

        if (!Directory.Exists(usersDirectory))
        {
            logger.LogInformation("Users directory not found at {Path}. Skipping user seeding", usersDirectory);
            return;
        }

        var userDirectories = Directory.GetDirectories(usersDirectory);
        logger.LogInformation("Processing {Count} user directories", userDirectories.Length);

        var total = userDirectories.Length;
        var processed = 0;

        foreach (var userDirectory in userDirectories)
        {
            var userEmail = Path.GetFileName(userDirectory);

            if (!IsValidEmail(userEmail))
            {
                logger.LogWarning(
                    "Skipping directory '{Name}' — not a valid email address",
                    userEmail);
                continue;
            }

            await SeedUserAsync(userDirectory, userEmail, cancellationToken);
            processed++;
            logger.LogInformation("Progress: {Processed}/{Total} users processed", processed, total);
        }
    }

    private async Task SeedUserAsync(string userDirectory, string userEmail, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing user {UserEmail}", userEmail);

        var userJsonPath = Path.Combine(userDirectory, "user.json");
        if (!File.Exists(userJsonPath))
        {
            logger.LogWarning(
                "Skipping user '{UserEmail}' — user.json not found at {Path}",
                userEmail, userJsonPath);
            return;
        }

        var dbUser = await ReadUserAsync(userJsonPath, userEmail, cancellationToken);

        var permissionsJsonPath = Path.Combine(userDirectory, "permissions.json");
        var seedPermissions = await ReadPermissionsAsync(permissionsJsonPath, dbUser.UserId, cancellationToken);

        ValidateSeedData(dbUser, seedPermissions, userEmail, userDirectory);

        var existing = await userRepository.GetAsync(dbUser.UserId, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "User {UserId} ({UserEmail}) already exists — skipping",
                dbUser.UserId, userEmail);
            return;
        }

        await userRepository.AddAsync(dbUser, cancellationToken);
        logger.LogInformation("Created user {UserId} ({UserEmail})", dbUser.UserId, userEmail);

        await SeedPermissionsAsync(seedPermissions, dbUser, cancellationToken);
    }

    private async Task SeedPermissionsAsync(
        List<SeedPermission> seedPermissions,
        DbUser user,
        CancellationToken cancellationToken)
    {
        if (seedPermissions.Count == 0)
        {
            logger.LogInformation("permissions.json is empty for user {UserId} — no permissions to seed", user.UserId);
            return;
        }

        var allRoles = await roleRepository.GetAllAsync(cancellationToken);

        foreach (var seedPerm in seedPermissions)
        {
            var tenant = await tenantRepository.GetAsync(seedPerm.TenantId, cancellationToken);
            if (tenant is null)
            {
                logger.LogWarning(
                    "Tenant {TenantId} not found — skipping permission for user {UserId}",
                    seedPerm.TenantId, user.UserId);
                continue;
            }

            var existing = await permissionRepository.GetAsync(user.UserId, seedPerm.TenantId, cancellationToken);
            if (existing is not null)
            {
                logger.LogInformation(
                    "Permission already exists for user {UserId} in tenant {TenantId} — skipping",
                    user.UserId, seedPerm.TenantId);
                continue;
            }

            var roleAssignments = ResolveRoleAssignments(seedPerm.RoleAssignments, allRoles, seedPerm.TenantId);

            var now = DateTimeOffset.UtcNow;
            var permission = new DbPermission
            {
                UserId = user.UserId,
                TenantId = seedPerm.TenantId,
                RoleAssignments = roleAssignments,
                CreatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedAt = now,
                UpdatedBy = Guid.Empty
            };

            await permissionRepository.AddAsync(permission, cancellationToken);
            logger.LogInformation(
                "Created permission for user {UserId} in tenant {TenantId}",
                user.UserId, seedPerm.TenantId);
        }
    }

    private List<DbRoleAssignment> ResolveRoleAssignments(
        IList<SeedRoleAssignment> seedRoleAssignments,
        IReadOnlyList<Infrastructure.Entities.Models.DbRoles.DbRole> allRoles,
        string tenantId)
    {
        var result = new List<DbRoleAssignment>();

        foreach (var seed in seedRoleAssignments)
        {
            var hasName = !string.IsNullOrEmpty(seed.RoleName);
            var hasId = !string.IsNullOrEmpty(seed.RoleId);

            if (hasName && hasId)
            {
                throw new InvalidOperationException(
                    $"RoleAssignment in tenant {tenantId} has both roleName '{seed.RoleName}' and roleId '{seed.RoleId}' — use one or the other");
            }

            if (!hasName && !hasId)
            {
                throw new InvalidOperationException(
                    $"RoleAssignment in tenant {tenantId} must have either roleName or roleId");
            }

            var role = hasId
                ? allRoles.FirstOrDefault(r => string.Equals(r.RoleId, seed.RoleId, StringComparison.OrdinalIgnoreCase))
                : allRoles.FirstOrDefault(r =>
                    string.Equals(r.Name, seed.RoleName, StringComparison.OrdinalIgnoreCase));

            if (role is null)
            {
                var ref_ = hasId ? $"ID '{seed.RoleId}'" : $"name '{seed.RoleName}'";
                throw new InvalidOperationException(
                    $"Role with {ref_} not found for tenant {tenantId}");
            }

            result.Add(new DbRoleAssignment
            {
                RoleId = role.RoleId,
                ValidFrom = seed.ValidFrom,
                ValidTo = seed.ValidTo
            });

            logger.LogDebug(
                "Resolved role {Ref} → {RoleId} for tenant {TenantId}",
                hasId ? seed.RoleId : seed.RoleName, role.RoleId, tenantId);
        }

        return result;
    }

    private static void ValidateSeedData(
        DbUser user,
        List<SeedPermission> permissions,
        string userEmail,
        string userDirectory)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(user.UserId))
        {
            errors.Add("UserId is null or empty");
        }

        if (string.IsNullOrWhiteSpace(user.Firstname))
        {
            errors.Add("Firstname is null or empty");
        }

        if (string.IsNullOrWhiteSpace(user.Lastname))
        {
            errors.Add("Lastname is null or empty");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            errors.Add("Email is null or empty");
        }
        else if (!string.Equals(user.Email, userEmail, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Email mismatch: JSON has '{user.Email}', directory name is '{userEmail}'");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Seed data validation failed for user '{userEmail}' in '{userDirectory}':{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors.Select(e => $"  - {e}")));
        }
    }

    private static async Task<DbUser> ReadUserAsync(
        string userJsonPath,
        string userEmail,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(userJsonPath, cancellationToken);

        DbUser? dbUser;
        try
        {
            dbUser = JsonConvert.DeserializeObject<DbUser>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize '{userJsonPath}' for user '{userEmail}'", ex);
        }

        if (dbUser is null)
        {
            throw new InvalidOperationException(
                $"user.json deserialized to null for user '{userEmail}' at path: {userJsonPath}");
        }

        return dbUser;
    }

    private static async Task<List<SeedPermission>> ReadPermissionsAsync(
        string permissionsJsonPath,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(permissionsJsonPath))
        {
            throw new InvalidOperationException(
                $"permissions.json is required but not found for user '{userId}' at path: {permissionsJsonPath}");
        }

        var json = await File.ReadAllTextAsync(permissionsJsonPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                $"permissions.json is empty for user '{userId}' at path: {permissionsJsonPath}. Use [] for no permissions");
        }

        List<SeedPermission>? result;
        try
        {
            result = JsonConvert.DeserializeObject<List<SeedPermission>>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize permissions.json for user '{userId}' at path: {permissionsJsonPath}", ex);
        }

        return result
               ?? throw new InvalidOperationException(
                   $"permissions.json deserialized to null for user '{userId}' at path: {permissionsJsonPath}");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            return new MailAddress(email).Address == email;
        }
        catch
        {
            return false;
        }
    }
}
