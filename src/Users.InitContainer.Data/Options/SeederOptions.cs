using System.ComponentModel.DataAnnotations;

namespace Users.InitContainer.Data.Options;

public sealed record SeederOptions
{
    public const string ConfigSectionName = "SeederOptions";

    [Required] public required bool TenantsSeed { get; init; }

    [Required] public required bool UsersSeed { get; init; }

    /// <summary>
    /// Path to the folder with JSON seed files.
    /// Structure: users-db/tenants.json | users-db/users/{user@email.com}/user.json | users-db/users/{user@email.com}/permissions.json
    /// </summary>
    [Required]
    public required string SeedDataFilePath { get; init; }
}
