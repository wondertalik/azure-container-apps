using System.Diagnostics;

namespace ManualContainerAppJob.Diagnostics;

/// <summary>
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public class ManualContainerAppJobInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new(nameof(ManualContainerAppJobInstrumentation));

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}