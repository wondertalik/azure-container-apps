using Libraries.Shared.Interfaces;
using Newtonsoft.Json;
using Users.Shared.Enums;

namespace Users.Infrastructure.Entities.Models.DbTenants;

public sealed record DbTenant : ICreatable, IUpdatable, ISoftDeletable, ILockable
{
    /// <summary>
    /// Id of Tenant
    /// </summary>
    [JsonProperty("id")]
    public required string TenantId { get; set; }

    /// <summary>d
    /// Name of Tenant
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Parent tenant id
    /// </summary>
    [JsonProperty("parentId")]
    public required string ParentId { get; set; } = Guid.Empty.ToString();

    /// <summary>
    /// List of children tenant ids
    /// </summary>
    [JsonProperty("childIds")]
    public IReadOnlyList<string> ChildIds { get; set; } = new List<string>();

    /// <summary>
    /// Company type
    /// </summary>
    [JsonProperty("tenantType")]
    public required TenantType TenantType { get; set; }

    /// <summary>
    /// When the tenant was created
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Who created the tenant
    /// </summary>
    [JsonProperty("createdBy")]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// When the tenant was last updated
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the tenant
    /// </summary>
    [JsonProperty("updatedBy")]
    public Guid UpdatedBy { get; set; }

    /// <summary>
    /// When the tenant was soft-deleted
    /// </summary>
    [JsonProperty("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Who soft-deleted the tenant
    /// </summary>
    [JsonProperty("deletedBy")]
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// When the tenant was locked for async operations
    /// </summary>
    [JsonProperty("lockedAt")]
    public DateTimeOffset? LockedAt { get; set; }
}
