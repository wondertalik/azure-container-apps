namespace Libraries.Shared.Services;

public sealed class CurrentDateTimeService : ICurrentDateTimeService
{
    public DateTimeOffset UtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
