using System.Diagnostics;

namespace Users.InitContainer.Diagnostics;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UsersInitContainerInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new("UsersInitContainer");

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
