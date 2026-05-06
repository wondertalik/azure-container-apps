# Copilot Instructions

## Project Overview

Azure Container Apps demo with .NET 8 — two containerized microservices with enterprise-grade observability (OpenTelemetry, Sentry, Azure Monitor) and a Users domain backed by CosmosDB.

## Build & Test

```bash
dotnet build
dotnet test

# Run a specific test project
dotnet test tests/HttpApi.IntegrationTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~MyTestName"
```

## Architecture

### Services

| Project | Type | Local port(s) |
|---|---|---|
| `src/FunctionApp1` | Azure Functions v4 Isolated | 7263 |
| `src/HttpApi` | ASP.NET Core Web API | HTTP 5238 / HTTPS 7125 |
| `src/Shared.Observability` | Shared library (OTel + Sentry + Azure Monitor) | — |
| `src/Libraries.Shared` | Generic helpers and interfaces | — |
| `src/Libraries.Shared.CosmosDb` | Reusable multi-database CosmosDB infrastructure | — |
| `src/Users.InitContainer` | One-shot init/migration job | — |
| `src/Users.InitContainer.Data` | Seeders and seed models | — |
| `src/Users.Infrastructure.CosmosDb` | CosmosDB implementations and migrations | — |
| `src/Users.Infrastructure.Contracts` | Repository interfaces | — |
| `src/Users.Infrastructure.Entities` | CosmosDB entity models | — |
| `src/Users.Authorization.Constants` | Authorization action and CosmosDB constants | — |
| `src/Users.Shared` | Shared enums | — |

### Users Module Flow

The Users domain is split across thin layers:
- **Entities** define DB models (`DbUser`, `DbTenant`, `DbRole`, etc.)
- **Contracts** define repository interfaces
- **CosmosDB** implements repositories and the migration system
- **InitContainer** runs once at startup: creates database/containers → applies migrations → seeds data

### Observability Pipeline

All telemetry is wired via `Shared.Observability` and routed through the OTEL Collector (`otel-collector-config.yaml`) to Jaeger (traces), Prometheus (metrics), and Seq (logs). The Aspire Dashboard (port 18888) is a lightweight local alternative.

Every service registers a custom `*Instrumentation` class in DI that holds its `ActivitySource`. The source name must be registered with `traceBuilder.AddSource(nameof(*Instrumentation))` in `Program.cs`.

All three telemetry integrations are **opt-in via configuration**:
- OTel → enabled when `OTELCOL_URL` is set
- Sentry → enabled when `Sentry:Dsn` is set
- Azure Monitor → enabled when `AzureMonitor:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` is set

Health check paths (`/healthz`, `/api/health`) and internal SDK traffic (Sentry, Seq, App Insights, Azure Functions host) are excluded from traces inside `Shared.Observability`.

## Key Conventions

### Options registration

Always use the `AddOptionsAndValidateOnStart<T>()` extension from `Libraries.Shared.Helpers` — it binds, validates data annotations, and validates on startup:

```csharp
services.AddOptionsAndValidateOnStart<MyOptions>(configuration, MyOptions.ConfigSectionName);
```

### CosmosDB infrastructure

`Libraries.Shared.CosmosDb` provides the reusable multi-database infrastructure. Each module registers its own database by calling `ConfigureDatabase()` on the injected `CosmosDbConfigurator`. Call `services.AddCosmosDb()` (from `Libraries.Shared.CosmosDb`) to register the infrastructure.

The Users module wires its CosmosDB config in two places:
- **`DependencyInjection.cs`** (`AddUsersCosmosDb`) — registers `AddCosmosDb()`, binds `UsersInfrastructureCosmosDbOptions`, and registers repositories
- **`CosmosDbExtensions.cs`** (`UseUsersCosmosDb`) — configures containers on the `CosmosDbConfigurator`; must be called on the built host before `UseUsersCosmosDbAsync`

`UsersInfrastructureCosmosDbOptions` (config section `Users:UsersInfrastructureCosmosDbOptions`) has these required properties:

| Property | Type | Notes |
|---|---|---|
| `ConnectionString` | `string` | CosmosDB connection string |
| `DatabaseId` | `string` | Database name (e.g. `users-db`) |
| `Throughput` | `int` | Autoscale RU/s (e.g. `400`) |
| `UseIntegratedCache` | `bool` | `true` → `ConnectionMode.Gateway` (required for integrated cache); `false` → `ConnectionMode.Direct` |
| `IgnoreSslCertificateValidation` | `bool` | `true` for local Docker emulator only |

`appsettings.json` documents the shape with default values. Override via .NET user secrets (local) or environment variables (Docker/CI).

### CosmosDB repositories

All entity repositories extend `SoftDeleteCosmosRepository<T>` where `T : ISoftDeletable`. Entities implement `ISoftDeletable` (`DeletedAt`, `DeletedBy`). The base class handles `AddAsync`, `UpdateAsync`, soft-delete, and batch transactional operations.

Container names and partition keys are defined in `Users.Authorization.Constants/UsersCosmosDbConstants.cs`. Adding a new entity requires:

1. A constants entry in `UsersCosmosDbConstants`
2. A `.Configure<DbMyEntity>(...)` block in `CosmosDbExtensions.ConfigureUsersCosmosDbContainers`
3. A repository registration in `DependencyInjection.AddUsersCosmosDb`

```csharp
db.ContainerBuilder
    .Configure<DbMyEntity>(o => o
        .WithName(UsersCosmosDbConstants.MyEntities.Name)
        .WithPartitionKeyPath(UsersCosmosDbConstants.MyEntities.PartitionKey)
        .WithPrimaryKey(e => e.MyEntityId)
        .WithPartitionKey(e => e.TenantId))
```

### Authorization action constants

Action constants live in `Users.Authorization.Constants/Actions/` and are split by domain:

| Class | Contents |
|---|---|
| `UserActions` | `ModuleUsers`, `UsersView*`, `UsersAdd*`, `UsersEdit*`, `UsersAssign*`, `UsersDelete*`, `UsersEditOwnProfile` |
| `TenantActions` | `TenantsView` |
| `AuthActions` | `AuthGetRoles`, `AuthGetRolesAllTenants`, `AuthGetActions` |

### Migrations

Migrations are extracted to a separate project (`Users.Infrastructure.CosmosDb.Migrations`). Implement `IMigration` with a date-time-prefixed version string and register it via `AddUsersCosmosDbMigrations()`:

```csharp
// In Users.Infrastructure.CosmosDb.Migrations/DependencyInjection.cs
services.AddSingleton<IMigration, V20250501_202100_InitialSeed>();
```

Naming convention: `V{yyyyMMdd}_{HHmmss}_{Description}`.

The migration service is registered in `Users.Infrastructure.CosmosDb/DependencyInjection.cs` and resolves all `IMigration` implementations via constructor injection. Call `.ApplyUsersMigrationsAsync()` on the host to execute pending migrations in order by version.

### FunctionApp1 local settings

`local.settings.json` is git-ignored. On first clone, copy the template:
```bash
cp src/FunctionApp1/local.settings.template.json src/FunctionApp1/local.settings.json
```

Functions host/binding settings (e.g., `AzureWebJobsStorage`) **must** go in `local.settings.json` — the Functions runtime does not read .NET user-secrets. Use .NET user-secrets only for application-level settings read through the standard `IConfiguration` pipeline. When adding a new required key, add a placeholder to `local.settings.template.json` and commit it.

### Console app configuration (Users.InitContainer)

Console applications must explicitly load user secrets by calling `builder.Configuration.AddUserSecrets<Program>()` after `Host.CreateApplicationBuilder(args)`. Unlike ASP.NET Core apps, user secrets are not auto-loaded based on environment. This is required for local development to resolve configuration from .NET user-secrets.

### Docker image builds

Both images require cert build secrets. FunctionApp1 additionally requires a `CERT_HASH` build arg:

```bash
# FunctionApp1
docker buildx build --platform linux/amd64 --progress plain \
  --build-arg BUILD_CONFIGURATION=Release \
  --build-arg CERT_HASH=$(cat ./certs/dev.crt ./certs/dev.key | sha256sum | cut -d' ' -f1) \
  --secret id=dev-crt,src=./certs/dev.crt \
  --secret id=dev-key,src=./certs/dev.key \
  -t my-func-app-1:1.0.0 -f src/FunctionApp1/Dockerfile .

# HttpApi (multi-platform)
docker buildx build --platform linux/amd64,linux/arm64 --progress plain \
  --build-arg BUILD_CONFIGURATION=Release \
  --secret id=dev-crt,src=./certs/dev.crt \
  --secret id=dev-key,src=./certs/dev.key \
  -t my-httpapi:1.0.0 -f src/HttpApi/Dockerfile .
```

### Conventional commits

`release-please` drives versioning from commit types. Supported types beyond the standard set:

| Type | Changelog section |
|---|---|
| `hotfix` | Hotfixes |
| `agents` | Agents (AI-assisted changes) |
| `tests` | Tests |
| `style` | Code Style |

Version is stored in `version.txt` and `.release-please-manifest.json`.

## Local Dev Ports

| Service | Port |
|---|---|
| Aspire Dashboard | 18888 |
| Jaeger UI | 16686 |
| Seq | 5384 |
| Prometheus | 9090 |
| cAdvisor | 8083 |
| OTLP gRPC | 4317 |
| OTLP HTTP | 4318 |
