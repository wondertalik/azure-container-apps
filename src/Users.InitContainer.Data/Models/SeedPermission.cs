using Newtonsoft.Json;

namespace Users.InitContainer.Data.Models;

public sealed class SeedPermission
{
    [JsonProperty("tenantId")] public required string TenantId { get; set; }

    [JsonProperty("roleAssignments")]
    public IList<SeedRoleAssignment> RoleAssignments { get; set; } = new List<SeedRoleAssignment>();
}
