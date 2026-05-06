using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbMigrations;

public sealed record DbMigration
{
    [JsonProperty("id")] public required string Id { get; set; }

    [JsonProperty("version")] public required string Version { get; set; }
}
