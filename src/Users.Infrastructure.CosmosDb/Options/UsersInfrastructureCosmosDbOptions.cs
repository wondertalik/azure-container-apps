using System.ComponentModel.DataAnnotations;

namespace Users.Infrastructure.CosmosDb.Options;

public sealed record UsersInfrastructureCosmosDbOptions
{
    public const string ConfigSectionName = "Users:UsersInfrastructureCosmosDbOptions";

    [Required] public required string ConnectionString { get; init; }

    [Required] public required string DatabaseId { get; init; }

    [Required] public required int Throughput { get; init; }

    [Required] public required bool UseIntegratedCache { get; init; }

    public bool IgnoreSslCertificateValidation { get; init; }
}
