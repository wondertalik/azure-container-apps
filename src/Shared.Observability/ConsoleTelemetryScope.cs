using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Observability.Options;

namespace Shared.Observability;

/// <summary>
/// Disposable telemetry scope for console applications and Container Apps Jobs.
/// Uses standalone TracerProvider and MeterProvider builders that flush on disposal,
/// ensuring telemetry is exported before the process exits.
/// </summary>
public sealed class ConsoleTelemetryScope : IDisposable
{
    private readonly TracerProvider _tracerProvider;
    private readonly MeterProvider _meterProvider;

    private ConsoleTelemetryScope(TracerProvider tracerProvider, MeterProvider meterProvider)
    {
        _tracerProvider = tracerProvider;
        _meterProvider = meterProvider;
    }

    /// <summary>
    /// Creates a telemetry scope with tracing and metrics configured from the application configuration.
    /// </summary>
    /// <param name="configuration">Application configuration (reads OTEL_SERVICE_NAME, OTEL_SERVICE_VERSION, OTELCOL_URL).</param>
    /// <param name="configureTracing">Optional callback to add custom sources, instrumentations, or exporters to the tracer.</param>
    /// <param name="configureMetrics">Optional callback to add custom meters or instrumentations to the meter provider.</param>
    public static ConsoleTelemetryScope Create(
        IConfiguration configuration,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        string serviceName = configuration.GetValue<string>("OTEL_SERVICE_NAME") ?? "UnknownConsoleApp";
        string serviceVersion = configuration.GetValue<string>("OTEL_SERVICE_VERSION") ?? "1.0.0";
        string? otelColUrl = configuration.GetValue<string>("OTELCOL_URL");

        var otelOltpTracingOptions = configuration
            .GetSection(OtelOltpTracingOptions.ConfigSectionName).Get<OtelOltpTracingOptions>();
        var otelOltpMetricsOptions = configuration
            .GetSection(OtelOltpMetricsOptions.ConfigSectionName).Get<OtelOltpMetricsOptions>();

        // Build TracerProvider. serviceName is used only for the resource;
        // callers register their own activity sources via configureTracing.
        TracerProviderBuilder traceBuilder = Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation(options =>
            {
                options.FilterHttpRequestMessage = HttpRequestMessageFilter.ShouldTrace;
            })
            .ConfigureResource(resource =>
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion));

        configureTracing?.Invoke(traceBuilder);

        if (!string.IsNullOrWhiteSpace(otelColUrl))
        {
            traceBuilder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otelColUrl);
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        if (otelOltpTracingOptions?.ConsoleExporter ?? false)
        {
            traceBuilder.AddConsoleExporter();
        }

        // Build MeterProvider
        MeterProviderBuilder meterBuilder = Sdk.CreateMeterProviderBuilder()
            .AddMeter("System.Net.Http")
            .AddMeter("System.Runtime")
            .ConfigureResource(resource =>
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion));

        configureMetrics?.Invoke(meterBuilder);

        if (!string.IsNullOrWhiteSpace(otelColUrl))
        {
            meterBuilder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otelColUrl);
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        if (otelOltpMetricsOptions?.ConsoleExporter ?? false)
        {
            meterBuilder.AddConsoleExporter();
        }

        return new ConsoleTelemetryScope(traceBuilder.Build(), meterBuilder.Build());
    }

    public void Dispose()
    {
        _tracerProvider.Dispose();
        _meterProvider.Dispose();
    }
}
