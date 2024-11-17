namespace Shared.Observability.Options;

public class OtelOltpTracingOptions
{
    public const string ConfigSectionName = "OtelOltpTracingOptions";
    
    public bool ConsoleExporter { get; init; }
}