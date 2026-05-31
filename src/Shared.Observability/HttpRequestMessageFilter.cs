namespace Shared.Observability;

/// <summary>
/// Shared HTTP request message filter for OpenTelemetry tracing.
/// Excludes internal SDK traffic that should not be traced.
/// </summary>
public static class HttpRequestMessageFilter
{
    /// <summary>
    /// Determines whether an HTTP request should be traced.
    /// Applies common exclusions for internal SDK traffic.
    /// </summary>
    public static bool ShouldTrace(HttpRequestMessage message)
    {
        return ShouldTrace(message, additionalFilter: null);
    }

    /// <summary>
    /// Determines whether an HTTP request should be traced.
    /// Applies common exclusions plus an optional additional filter for app-specific rules.
    /// </summary>
    /// <param name="message">The HTTP request message to evaluate.</param>
    /// <param name="additionalFilter">
    /// Optional additional filter. Return false to exclude the request from tracing.
    /// Invoked only if the common exclusions did not already exclude the request.
    /// </param>
    public static bool ShouldTrace(HttpRequestMessage message, Func<HttpRequestMessage, bool>? additionalFilter)
    {
        if (message.RequestUri?.Host.Contains(".sentry.io") ?? false)
        {
            return false;
        }

        if (message.RequestUri?.Host.Contains(".livediagnostics.monitor.azure.com") ?? false)
        {
            return false;
        }

        if (message.RequestUri?.Host.Contains("applicationinsights.azure.com") ?? false)
        {
            return false;
        }

        if (additionalFilter is not null)
        {
            return additionalFilter(message);
        }

        return true;
    }
}
