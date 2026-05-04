using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbTenants;
using Users.InitContainer.Data.Options;

namespace Users.InitContainer.Data.Seeders;

public sealed class TenantSeeder(
    ITenantRepository tenantRepository,
    IOptions<SeederOptions> seederOptions,
    ILogger<TenantSeeder> logger)
{
    private readonly SeederOptions _options = seederOptions.Value;

    public async Task SeedIfEnabledAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.TenantsSeed)
        {
            logger.LogInformation("Tenant seeding is disabled via SeederOptions.TenantsSeed");
            return;
        }

        logger.LogInformation(
            "Starting tenant seeding from {SeedDataFilePath}/users-db/tenants.json",
            _options.SeedDataFilePath);

        await SeedFromJsonAsync(cancellationToken);

        logger.LogInformation("Tenant seeding completed successfully");
    }

    private async Task SeedFromJsonAsync(CancellationToken cancellationToken)
    {
        string seedPath = _options.SeedDataFilePath;
        if (!Path.IsPathRooted(seedPath))
        {
            seedPath = Path.Combine(AppContext.BaseDirectory, seedPath);
        }

        string usersDbPath = Path.Combine(seedPath, "users-db");
        string tenantsJsonPath = Path.Combine(usersDbPath, "tenants.json");

        if (!File.Exists(tenantsJsonPath))
        {
            logger.LogWarning("tenants.json not found at {Path}. Skipping tenant seeding", tenantsJsonPath);
            return;
        }

        string json = await File.ReadAllTextAsync(tenantsJsonPath, cancellationToken);
        var allTenants = JsonConvert.DeserializeObject<List<DbTenant>>(json, new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });

        if (allTenants == null || allTenants.Count == 0)
        {
            logger.LogInformation("No tenants found in tenants.json");
            return;
        }

        logger.LogInformation("Read {Count} tenant(s) from tenants.json", allTenants.Count);
        await ProcessHierarchicallyAsync(allTenants, cancellationToken);
    }

    private async Task ProcessHierarchicallyAsync(List<DbTenant> allTenants, CancellationToken cancellationToken)
    {
        var uniqueTenants = new List<DbTenant>();
        var seenIds = new HashSet<string>();

        foreach (var tenant in allTenants)
        {
            if (seenIds.Add(tenant.TenantId))
            {
                uniqueTenants.Add(tenant);
            }
            else
            {
                logger.LogWarning(
                    "Duplicate tenant ID {TenantId} ({Name}) — skipping",
                    tenant.TenantId, tenant.Name);
            }
        }

        var lookup = uniqueTenants.ToDictionary(t => t.TenantId);
        var processed = new HashSet<string>();

        logger.LogInformation("Processing {Count} unique tenants", uniqueTenants.Count);

        foreach (var tenant in uniqueTenants)
        {
            if (!processed.Contains(tenant.TenantId))
            {
                await ProcessRecursivelyAsync(tenant, lookup, processed, cancellationToken);
            }
        }

        logger.LogInformation("Completed processing {Count} tenants", processed.Count);
    }

    private async Task ProcessRecursivelyAsync(
        DbTenant tenant,
        Dictionary<string, DbTenant> lookup,
        HashSet<string> processed,
        CancellationToken cancellationToken)
    {
        if (processed.Contains(tenant.TenantId))
        {
            return;
        }

        // Ensure parent is processed first
        if (!string.IsNullOrWhiteSpace(tenant.ParentId) && lookup.TryGetValue(tenant.ParentId, out var parent))
        {
            if (!processed.Contains(parent.TenantId))
            {
                await ProcessRecursivelyAsync(parent, lookup, processed, cancellationToken);
            }
        }

        await CreateIfNotExistsAsync(tenant, cancellationToken);
        processed.Add(tenant.TenantId);

        foreach (var child in lookup.Values.Where(t => t.ParentId == tenant.TenantId))
        {
            await ProcessRecursivelyAsync(child, lookup, processed, cancellationToken);
        }
    }

    private async Task CreateIfNotExistsAsync(DbTenant tenant, CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing tenant {TenantId} ({Name})", tenant.TenantId, tenant.Name);

        var existing = await tenantRepository.GetAsync(tenant.TenantId, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Tenant {TenantId} ({Name}) already exists — skipping creation",
                tenant.TenantId, tenant.Name);

            if (!string.IsNullOrWhiteSpace(tenant.ParentId) && tenant.ParentId != Guid.Empty.ToString())
            {
                await UpdateParentChildIdsAsync(tenant.ParentId, tenant.TenantId, cancellationToken);
            }

            return;
        }

        await tenantRepository.AddAsync(tenant, cancellationToken);
        logger.LogInformation("Created tenant {TenantId} ({Name})", tenant.TenantId, tenant.Name);

        if (!string.IsNullOrWhiteSpace(tenant.ParentId) && tenant.ParentId != Guid.Empty.ToString())
        {
            await UpdateParentChildIdsAsync(tenant.ParentId, tenant.TenantId, cancellationToken);
        }
    }

    private async Task UpdateParentChildIdsAsync(string parentId, string childId, CancellationToken cancellationToken)
    {
        var parent = await tenantRepository.GetAsync(parentId, cancellationToken);
        if (parent is null)
        {
            logger.LogWarning(
                "Parent tenant {ParentId} not found — cannot update ChildIds for child {ChildId}",
                parentId, childId);
            return;
        }

        if (parent.ChildIds.Contains(childId))
        {
            return;
        }

        var updatedChildIds = parent.ChildIds.ToList();
        updatedChildIds.Add(childId);
        await tenantRepository.UpdateAsync(parent with { ChildIds = updatedChildIds }, cancellationToken);
        logger.LogDebug("Updated parent {ParentId} ChildIds with {ChildId}", parentId, childId);
    }
}
