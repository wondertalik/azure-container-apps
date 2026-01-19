using System.ComponentModel.DataAnnotations;

namespace UtilsActions.Options;

public sealed record ServiceBusOptions
{
    public const string ConfigSectionName = "ServiceBusOptions";

    [Required] public string ConnectionString { get; init; } = null!;
}
