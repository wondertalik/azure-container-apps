using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbActions;

public sealed record DbAction
{
    /// <summary>
    /// Id of Action and the PartitionKey also
    /// </summary>
    [JsonProperty("id")]
    public required string ActionId { get; set; }

    /// <summary>
    /// Name of the action
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; set; }
}
