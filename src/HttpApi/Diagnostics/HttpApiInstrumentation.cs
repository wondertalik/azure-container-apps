using System.Diagnostics;

namespace HttpApi.Diagnostics;

/// <summary>
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public class HttpApiInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new(nameof(HttpApiInstrumentation));

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}