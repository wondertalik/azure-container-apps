using System.Diagnostics;
using ManualContainerAppJob.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Observability;

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

string serviceName = configurationRoot.GetValue<string>("OTEL_SERVICE_NAME") ?? "OptiLeadsInitContainer";
string serviceVersion = configurationRoot.GetValue<string>("OTEL_SERVICE_VERSION") ?? "1.0.0";
string? otelColUrl = configurationRoot.GetValue<string>("OTELCOL_URL");
Console.WriteLine(
    $"Using OpenTelemetry Collector URL: {otelColUrl}, Service Name: {serviceName}, Version: {serviceVersion}");

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

TracerProviderBuilder traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .AddSource(nameof(ManualContainerAppJobInstrumentation))
    .AddHttpClientInstrumentation()
    .ConfigureResource(resource =>
        resource.AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion));

if (!string.IsNullOrWhiteSpace(otelColUrl))
{
    traceProviderBuilder.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(otelColUrl);
        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
    });
}

TracerProvider tracerProvider = traceProviderBuilder.Build();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddHttpClient();

// Add services to the container.
OpenTelemetryBuilder otel = builder.Services.AddOpenTelemetry();
builder.Services.AddSingleton<ManualContainerAppJobInstrumentation>();
builder.Services.ConfigureOpenTelemetryResource(builder.Configuration, otel);
builder.Logging.AddOpenTelemetryLogsInstrumentation(builder.Configuration);

IHost app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var instrumentation = scope.ServiceProvider.GetRequiredService<ManualContainerAppJobInstrumentation>();
HttpClient httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();

using Activity? activity = instrumentation.ActivitySource.StartActivity("ManualContainerAppJob.Execution");

logger.LogInformation("Run ManualContainerAppJob...");

try
{
    httpClient.DefaultRequestHeaders.Add("User-Agent", "ManualContainerAppJob");

    HttpResponseMessage response = await httpClient.GetAsync("https://api.github.com/users/octocat");
    response.EnsureSuccessStatusCode();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while making the HTTP request");
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
}

// Dispose of the tracer provider
tracerProvider.Dispose();

logger.LogInformation("ManualContainerAppJob run completed successfully");

Thread.Sleep(5000); // Give some time for the flush to complete before exiting