namespace Shared.Observability.Options;

public class OtelOltpLogsOptions
{
    public const string ConfigSectionName = "OtelOltpLogsOptions";
    
    public bool ConsoleExporter { get; init; }
}