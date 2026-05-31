using System.Diagnostics;

namespace Users.InitContainer.Diagnostics;

/// <summary>
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UsersInitContainerInstrumentation : IDisposable
{
    public const string Name = nameof(UsersInitContainerInstrumentation);

    public ActivitySource ActivitySource { get; } = new(Name);

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
