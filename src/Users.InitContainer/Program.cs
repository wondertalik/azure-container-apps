using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Users.Infrastructure.CosmosDb;
using Users.Infrastructure.CosmosDb.Extensions;
using Users.Infrastructure.CosmosDb.Migrations;
using Users.Infrastructure.Contracts.Repositories;
using Users.InitContainer.Data;
using Users.InitContainer.Data.Seeders;
using Users.InitContainer.Diagnostics;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

string serviceName = configurationRoot.GetValue<string>("OTEL_SERVICE_NAME") ?? "UsersInitContainer";
string serviceVersion = configurationRoot.GetValue<string>("OTEL_SERVICE_VERSION") ?? "1.0.0";
string? otelColUrl = configurationRoot.GetValue<string>("OTELCOL_URL");

TracerProviderBuilder traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .AddSource("Azure.Cosmos.Operation")
    .AddHttpClientInstrumentation()
    .ConfigureResource(resource =>
        resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion));

if (!string.IsNullOrWhiteSpace(otelColUrl))
{
    traceProviderBuilder.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(otelColUrl);
        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
    });
}

using TracerProvider tracerProvider = traceProviderBuilder.Build();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services
    .AddSingleton<UsersInitContainerInstrumentation>()
    .AddUsersCosmosDb(builder.Configuration)
    .AddUsersCosmosDbMigrations()
    .AddUsersInitContainerData(builder.Configuration);

IHost app = builder.Build();

app.UseUsersCosmosDb();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var manager = app.Services.GetRequiredService<IUsersCosmosDbManagerRepository>();

if (configuration.GetValue("UsersDropDatabaseIfExists", false))
{
    await manager.DropDatabaseIfExistsAsync();
}

await manager.CreateDatabaseIfNotExistsAsync();

using IServiceScope scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var instrumentation = scope.ServiceProvider.GetRequiredService<UsersInitContainerInstrumentation>();

using var rootActivity = instrumentation.ActivitySource.StartActivity("UsersInitContainer.Run");

logger.LogInformation("UsersInitContainer started");

// Phase 1: apply migrations
using var migrationsActivity = instrumentation.ActivitySource.StartActivity("ApplyMigrations");
await app.ApplyUsersMigrationsAsync(CancellationToken.None);
migrationsActivity?.Stop();

// Phase 2: seed tenants
using var tenantSeedActivity = instrumentation.ActivitySource.StartActivity("SeedTenants");
var tenantSeeder = scope.ServiceProvider.GetRequiredService<TenantSeeder>();
await tenantSeeder.SeedIfEnabledAsync(CancellationToken.None);
tenantSeedActivity?.Stop();

// Phase 3: seed users
using var userSeedActivity = instrumentation.ActivitySource.StartActivity("SeedUsers");
var userSeeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();
await userSeeder.SeedIfEnabledAsync(CancellationToken.None);
userSeedActivity?.Stop();

rootActivity?.Stop();

logger.LogInformation("UsersInitContainer completed successfully");

await Task.Delay(5000); // allow OTEL flush before exit
