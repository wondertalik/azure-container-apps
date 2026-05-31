using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Observability;
using Users.Infrastructure.CosmosDb;
using Users.Infrastructure.CosmosDb.Extensions;
using Users.Infrastructure.CosmosDb.Migrations;
using Users.Infrastructure.Contracts.Repositories;
using Users.InitContainer.Data;
using Users.InitContainer.Data.Seeders;
using Users.InitContainer.Diagnostics;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

using ConsoleTelemetryScope telemetry = ConsoleTelemetryScope.Create(
    builder.Configuration,
    configureTracing: tracing =>
    {
        tracing.AddSource(UsersInitContainerInstrumentation.Name);
        tracing.AddSource("Azure.Cosmos.Operation");
    });

builder.Logging.AddOpenTelemetryLogsInstrumentation(builder.Configuration);

builder.Services
    .AddSingleton<UsersInitContainerInstrumentation>()
    .AddUsersCosmosDb(builder.Configuration)
    .AddUsersCosmosDbMigrations()
    .AddUsersInitContainerData(builder.Configuration);

using IHost app = builder.Build();

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
