using HttpApi.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Shared.Observability;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry(options =>
{
    options.UseOpenTelemetry();
    options.DisableSentryHttpMessageHandler = true;
});

// Add services to the container.
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
        traceBuilder.AddSentry();
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