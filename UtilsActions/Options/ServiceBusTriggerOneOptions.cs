using System.ComponentModel.DataAnnotations;

namespace UtilsActions.Options;

public sealed record ServiceBusTriggerOneOptions
{
    public const string ConfigSectionName = "ServiceBusTriggerOneOptions";

    [Required] public required string ServiceBusTriggerOneQueue { get; init; }
}
