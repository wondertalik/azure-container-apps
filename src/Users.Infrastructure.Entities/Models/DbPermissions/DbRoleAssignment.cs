using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbPermissions;

public sealed record DbRoleAssignment
{
    /// <summary>
    /// Id of role
    /// </summary>
    [JsonProperty(PropertyName = "roleId")]
    public required string RoleId { get; set; }

    /// <summary>
    /// Validity of assignment from, null means that it is valid since forever
    /// </summary>
    [JsonProperty(PropertyName = "validFrom")]
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>
    /// Validity of assignment to, null means that it is valid until forever
    /// </summary>
    [JsonProperty(PropertyName = "validTo")]
    public DateTimeOffset? ValidTo { get; set; }
}
