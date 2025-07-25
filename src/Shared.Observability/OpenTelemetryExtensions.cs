using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Observability.Options;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

namespace Shared.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAzureMonitor(this IServiceCollection services,
        IConfiguration configuration, OpenTelemetryBuilder otel,
        Action<AzureMonitorOptions>? azureMonitorOptions = null)
    {
        string? azureMonitorConnectionString = configuration.GetValue<string>("AzureMonitor:ConnectionString") ??
                                               configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
        switch (string.IsNullOrWhiteSpace(azureMonitorConnectionString))
        {
            case false:
                services.Configure<AzureMonitorOptions>(configuration.GetSection("AzureMonitor"));
                otel.UseAzureMonitor(options =>
                {
                    options.ConnectionString = azureMonitorConnectionString;
                    azureMonitorOptions?.Invoke(options);
                });
                break;
        }

        return services;
    }
    
    public static void AddOpenTelemetryLogsInstrumentation(this ILoggingBuilder builder,
        IConfiguration configuration, Action<OpenTelemetryLoggerOptions>? configureLoggerOptions = null)
    {
        var otelOltpLogsOptions = configuration
            .GetSection(OtelOltpLogsOptions.ConfigSectionName).Get<OtelOltpLogsOptions>();

        builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            configureLoggerOptions?.Invoke(options);
            
            string? otelcolUrl = configuration["OTELCOL_URL"];
            if (!string.IsNullOrEmpty(otelcolUrl))
            {
                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri($"{otelcolUrl}");
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;

                    string? seqApiKey = configuration["SEQ_API_KEY"];
                    if (string.IsNullOrEmpty(seqApiKey) is false)
                    {
                        const string headerKey = "X-Seq-ApiKey";

                        string formattedHeader = $"{headerKey}={seqApiKey}";
                        otlpOptions.Headers = formattedHeader;
                    }
                });
            }

            if (otelOltpLogsOptions?.ConsoleExporter ?? false)
                options.AddConsoleExporter();
        });
    }

    public static IServiceCollection AddOpenTelemetryMetricsInstrumentation(this IServiceCollection services,
        IConfiguration configuration, IOpenTelemetryBuilder otel,
        Action<MeterProviderBuilder>? configureMeterProviderBuilder = null)
    {
        otel.WithMetrics(meterBuilder =>
        {
            var otelOltpMetricsOptions = configuration
                .GetSection(OtelOltpMetricsOptions.ConfigSectionName).Get<OtelOltpMetricsOptions>();

            meterBuilder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Metrics provides by ASP.NET Core
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel");

            configureMeterProviderBuilder?.Invoke(meterBuilder);
            
            string? otelcolUrl = configuration["OTELCOL_URL"];

            if (!string.IsNullOrEmpty(otelcolUrl))
            {
                meterBuilder.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri($"{otelcolUrl}");
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;

                    string? seqApiKey = configuration["SEQ_API_KEY"];
                    if (string.IsNullOrEmpty(seqApiKey) is false)
                    {
                        const string headerKey = "X-Seq-ApiKey";

                        string formattedHeader = $"{headerKey}={seqApiKey}";
                        otlpOptions.Headers = formattedHeader;
                    }
                });
            }

            if (otelOltpMetricsOptions?.ConsoleExporter ?? false)
                meterBuilder.AddConsoleExporter();
        });

        return services;
    }

    public static IServiceCollection AddOpenTelemetryTracingInstrumentation(this IServiceCollection services,
        IConfiguration configuration, IOpenTelemetryBuilder otel,
        Action<TracerProviderBuilder>? configureTracerProviderBuilder = null)
    {
        otel.WithTracing(tracing =>
        {
            var otelOltpTracingOptions = configuration
                .GetSection(OtelOltpTracingOptions.ConfigSectionName).Get<OtelOltpTracingOptions>();

            AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

            tracing.AddGrpcClientInstrumentation();
            tracing.AddGrpcCoreInstrumentation();
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context =>
                {
                    var excludeSegments = new List<string>
                    {
                        "/metrics",
                        "/favicon.ico",
                        "/robots.txt",
                        "/healthz",
                        "/api/health"
                    };
                    bool excludeEvent = excludeSegments.Any(x => context.Request.Path.StartsWithSegments(x));
                    return !excludeEvent;
                };
            });
            tracing.AddHttpClientInstrumentation(options =>
            {
                options.FilterHttpRequestMessage = message =>
                {
                    //do not send seq request to jaeger
                    bool excludeSeqEvent = message.RequestUri?.AbsolutePath.Equals("/api/events/raw") ?? false;
                    if (excludeSeqEvent)
                        return false;

                    //do not send seq request to jaeger
                    bool excludeLiveDiagnosticsEvent =
                        message.RequestUri?.Host.Contains(".livediagnostics.monitor.azure.com") ?? false;
                    if (excludeLiveDiagnosticsEvent)
                        return false;

                    //do not send seq request to jaeger
                    bool excludeApplicationInsightsEvent =
                        message.RequestUri?.Host.Contains("applicationinsights.azure.com") ?? false;
                    if (excludeApplicationInsightsEvent)
                        return false;

                    bool excludeAzureFunctionsEvent =
                        message.RequestUri?.AbsolutePath.Equals("/AzureFunctionsRpcMessages.FunctionRpc/EventStream") ??
                        false;
                    if (excludeAzureFunctionsEvent)
                        return false;

                    bool excludeAzureFunctionsHostMeta = message.RequestUri?.Host.Equals("169.254.169.254") ?? false;
                    if (excludeAzureFunctionsHostMeta)
                        return false;

                    return true;
                };
            });

            configureTracerProviderBuilder?.Invoke(tracing);
            
            string? otelcolUrl = configuration["OTELCOL_URL"];

            if (!string.IsNullOrEmpty(otelcolUrl))
            {
                tracing.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri($"{otelcolUrl}");
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;

                    string? seqApiKey = configuration["SEQ_API_KEY"];
                    if (string.IsNullOrWhiteSpace(seqApiKey) is false)
                    {
                        const string headerKey = "X-Seq-ApiKey";

                        string formattedHeader = $"{headerKey}={seqApiKey}";
                        otlpOptions.Headers = formattedHeader;
                    }
                });
            }

            if (otelOltpTracingOptions?.ConsoleExporter ?? false)
                tracing.AddConsoleExporter();
        });

        return services;
    }

    public static IServiceCollection UseOpenTelemetryOltpExporter(
        this IServiceCollection services, IConfiguration configuration, IOpenTelemetryBuilder otel)
    {
        // Export OpenTelemetry data via OTLP, using env vars for the configuration
        string? otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        string? otelColUrl = configuration["OTELCOL_URL"];

        if (!string.IsNullOrWhiteSpace(otlpEndpoint) && string.IsNullOrWhiteSpace(otelColUrl))
        {
            otel.UseOtlpExporter();
        }

        return services;
    }

    public static IServiceCollection ConfigureOpenTelemetryResource(
        this IServiceCollection services, IConfiguration configuration, IOpenTelemetryBuilder otel)
    {
        string? serviceName = configuration["OTEL_SERVICE_NAME"];
        if (string.IsNullOrWhiteSpace(serviceName)) return services;
        
        string? serviceVersion = configuration["OTEL_SERVICE_VERSION"];

        otel.ConfigureResource(resource =>
        {
            resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion,
                autoGenerateServiceInstanceId: false);

            // Add custom attribute for environment
            Dictionary<string, object> attributes = new();

            string? environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (!string.IsNullOrWhiteSpace(environment))
                attributes.Add("deployment.environment", environment);

            resource.AddAttributes(attributes);
        });

        return services;
    }
}