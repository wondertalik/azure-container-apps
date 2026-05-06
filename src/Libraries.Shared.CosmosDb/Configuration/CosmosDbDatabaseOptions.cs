namespace Libraries.Shared.CosmosDb.Configuration;

public sealed class CosmosDbDatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseId { get; set; } = string.Empty;

    public bool IgnoreSslCertificateValidation { get; set; }

    public int Throughput { get; set; }

    public bool UseIntegratedCache { get; set; }

    public CosmosDbContainerBuilder ContainerBuilder { get; } = new();
}
