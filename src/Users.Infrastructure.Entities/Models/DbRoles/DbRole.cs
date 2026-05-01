using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbRoles;

public sealed record DbRole
{
    /// <summary>
    /// Id of Role
    /// </summary>
    [JsonProperty("id")]
    public required string RoleId { get; set; }

    /// <summary>
    /// Name of the role for each tenant
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Group of the role for each tenant
    /// </summary>
    [JsonProperty("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Id of Tenant, this is also the Partition Key
    /// </summary>
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = Guid.Empty.ToString();

    /// <summary>
    /// List of action ids that are assigned to this role
    /// </summary>
    [JsonProperty("actionIds")]
    public IReadOnlyList<string> ActionIds { get; set; } = new List<string>();
}
