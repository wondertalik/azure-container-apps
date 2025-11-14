using HttpApi.Diagnostics;
using OpenTelemetry;
using Sentry.Extensions.Logging;
using Sentry.OpenTelemetry;
using Shared.Observability;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string? sentryDsn = builder.Configuration.GetValue<string>("Sentry:Dsn");

if (!string.IsNullOrEmpty(sentryDsn))
{
    // Add services to the container.
    builder.Services.Configure<SentryLoggingOptions>(builder.Configuration.GetSection("Sentry"));

    builder.Logging.AddSentry(options =>
    {
        options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
    });
    
    // https://github.com/getsentry/sentry-dotnet/issues/4539
    // builder.Logging.Services.Configure<LoggerFilterOptions>(static (LoggerFilterOptions options) =>
    // {
    //     LoggerFilterRule rule = options.Rules.Single(static (LoggerFilterRule rule) => rule.ProviderName == "Sentry.AspNetCore.SentryAspNetCoreStructuredLoggerProvider");
    //     bool removed = options.Rules.Remove(rule);
    //     if (!removed)
    //     {
    //         throw new InvalidOperationException("Rule for Sentry's Structured Logs not found.");
    //     }
    //
    //     rule = new LoggerFilterRule(rule.ProviderName, "Sentry.ISentryClient", LogLevel.None, null);
    //     options.Rules.Add(rule);
    //     rule = new LoggerFilterRule(rule.ProviderName, "Sentry.AspNetCore.SentryMiddleware", LogLevel.None, null);
    //     options.Rules.Add(rule);
    // });
}

OpenTelemetryBuilder otel = builder.Services.AddOpenTelemetry();
builder.Logging.AddOpenTelemetryLogsInstrumentation(builder.Configuration);
builder.Services
    .AddAzureMonitor(builder.Configuration, otel)
    .ConfigureOpenTelemetryResource(builder.Configuration, otel)
    .AddOpenTelemetryMetricsInstrumentation(builder.Configuration, otel)
    .AddOpenTelemetryTracingInstrumentation(builder.Configuration, otel, traceBuilder =>
    {
        traceBuilder.AddSource(nameof(HttpApiInstrumentation));
        traceBuilder.AddSource("Azure.*");
        traceBuilder.AddSource(
            "Azure.Cosmos.Operation", // Cosmos DB source for operation level telemetry
            "Sample.Application"
        );
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            traceBuilder.AddSentry();
        }
    })
    .UseOpenTelemetryOltpExporter(builder.Configuration, otel);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();
app.MapHealthChecks("/healthz");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();