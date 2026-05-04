using Libraries.Shared.Interfaces;
using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbPermissions;

public sealed record DbPermission : ICreatable, IUpdatable, ISoftDeletable
{
    /// <summary>
    /// Id of User
    /// </summary>
    [JsonProperty("id")]
    public required string UserId { get; set; }

    /// <summary>
    /// Id of Tenant, this is also the Partition Key
    /// </summary>
    [JsonProperty("tenantId")]
    public required string TenantId { get; set; }

    /// <summary>
    /// Assigned roles
    /// </summary>
    [JsonProperty("roleAssignments")]
    public IReadOnlyList<DbRoleAssignment> RoleAssignments { get; set; } = new List<DbRoleAssignment>();

    [JsonProperty("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("createdBy")] public string CreatedBy { get; set; } = string.Empty;

    [JsonProperty("updatedAt")] public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("updatedBy")] public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was deleted
    /// </summary>
    [JsonProperty("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity
    /// </summary>
    [JsonProperty("deletedBy")]
    public string? DeletedBy { get; set; }
}
