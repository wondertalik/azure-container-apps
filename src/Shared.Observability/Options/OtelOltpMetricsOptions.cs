namespace Shared.Observability.Options;

public class OtelOltpMetricsOptions
{
    public const string ConfigSectionName = "OtelOltpMetricsOptions";
    
    public bool ConsoleExporter { get; init; }
}