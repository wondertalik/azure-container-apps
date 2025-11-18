using FunctionApp1;
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
using Sentry.Extensions.Logging;
using Sentry.OpenTelemetry;
using Shared.Observability;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
string? sentryDsn = builder.Configuration.GetValue<string>("Sentry:Dsn");

if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.Services.Configure<SentryLoggingOptions>(builder.Configuration.GetSection("Sentry"));
    builder.Logging.AddSentry(options =>
    {
        options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
    });
}
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
        
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            traceBuilder.AddSentry();
        }
    })
    .UseOpenTelemetryOltpExporter(builder.Configuration, otel);
builder.Services.ConfigureInstrumentation();

builder.Services.ConfigureServices(builder.Configuration);

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