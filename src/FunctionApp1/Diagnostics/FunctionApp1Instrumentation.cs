using System.Diagnostics;

namespace FunctionApp1.Diagnostics;

/// <summary>
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public class FunctionApp1Instrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new(nameof(FunctionApp1Instrumentation));

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}