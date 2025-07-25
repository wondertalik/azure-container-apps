using FunctionApp1.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Shared.Observability;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Configuration.AddUserSecrets<Program>();

// Export OpenTelemetry data via OTLP, using env vars for the configuration
OpenTelemetryBuilder otel = builder.Services.AddOpenTelemetry();
otel.UseFunctionsWorkerDefaults();
builder.Logging.AddOpenTelemetryLogsInstrumentation(builder.Configuration);
builder.Services
    .AddAzureMonitor(builder.Configuration, otel)
    .ConfigureOpenTelemetryResource(builder.Configuration, otel)
    .AddOpenTelemetryMetricsInstrumentation(builder.Configuration, otel)
    .AddOpenTelemetryTracingInstrumentation(builder.Configuration, otel, traceBuilder =>
    {
        traceBuilder.AddSource(nameof(FunctionApp1Instrumentation));
        traceBuilder.AddSource("Azure.*");
        traceBuilder.AddSource(
            "Azure.Cosmos.Operation", // Cosmos DB source for operation level telemetry
            "Sample.Application"
        );
        traceBuilder.SetSampler(new AlwaysOnSampler())
            .AddSource("Microsoft.Azure.Functions.Worker");
    })
    .UseOpenTelemetryOltpExporter(builder.Configuration, otel);

string? applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    // Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();

    builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
    {
        LoggerFilterRule? defaultRule =
            options.Rules.FirstOrDefault(rule =>
                rule.ProviderName ==
                "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

        if (defaultRule is not null)
        {
            options.Rules.Remove(defaultRule);
        }
    });
}

IHost host = builder.Build();

host.Run();