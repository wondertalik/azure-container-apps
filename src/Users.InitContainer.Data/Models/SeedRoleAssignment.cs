using Newtonsoft.Json;

namespace Users.InitContainer.Data.Models;

public sealed class SeedRoleAssignment
{
    [JsonProperty("roleId")] public string? RoleId { get; set; }

    [JsonProperty("roleName")] public string? RoleName { get; set; }

    [JsonProperty("validFrom")] public DateTimeOffset? ValidFrom { get; set; }

    [JsonProperty("validTo")] public DateTimeOffset? ValidTo { get; set; }
}
