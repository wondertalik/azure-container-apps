# Users Module — Planning, Documentation & Architecture

## Context

This is the first phase (clarification + documentation + diagrams) for a new **Users management module** in the Azure Container Apps .NET 8 solution. No code will be written until this document and `docs/users.md` are approved.

The module adds 9 projects to the existing solution under the "Users" solution folder:

| Project | Purpose |
|---|---|
| `Users.Authorization.Constants` | Action ID string constants + `Root` system constants + `UsersCosmosDbConstants` |
| `Users.Shared` | `TenantType` enum |
| `Users.Infrastructure.Entities` | CosmosDB entity records |
| `Users.Infrastructure.Contracts` | Repository interface definitions (+ `IUsersCosmosDbManagerRepository`) |
| `Libraries.Shared.CosmosDb` | Reusable multi-database CosmosDB infrastructure (`CosmosDbConfigurator`, `AddCosmosDb()`) |
| `Users.Infrastructure.CosmosDb` | CosmosDB repositories, options, migrations interfaces, DI wiring |
| `Users.Infrastructure.CosmosDb.Migrations` | Concrete migration implementations (`V20250501_202100_InitialSeed`) |
| `Users.InitContainer` | Console app — DB provisioning + seed |
| `Users.InitContainer.Data` | Seed data files, seeders, `SeederOptions` |

---

## Phase 1 Output: `docs/users.md`

The documentation file must cover all sections below with Mermaid diagrams.

### 1. Module Overview
- Purpose, scope, what problems it solves
- RBAC model summary (users → tenants → roles → actions)
- Relationship to existing services (HttpApi, FunctionApp1)

### 2. Domain Model — Entities

Document each entity: CosmosDB container, partition key, `id` field, audit interfaces, and access patterns.

#### Entities and their fixes vs user-provided definitions

| Entity | Class name | Container | Partition Key | Doc `id` | Implements | Fixes needed |
|---|---|---|---|---|---|---|
| `DbUser` | `DbUser` | `users` | `/id` | `UserId` | `ICreatable, IUpdatable, ISoftDeletable, ILockable` | — |
| `DbTenant` | `DbTenant` | `tenants` | `/id` | `TenantId` | `ICreatable, IUpdatable, ISoftDeletable, ILockable` | `ParentId` should be `string?` (null = root) |
| `DbRole` | `DbRole` | `roles` | `/tenantId` | `RoleId` | — | — |
| `DbAction` | `DbAction` | `actions` | `/id` | `ActionId` | — | Change from `internal` to `public` |
| `DbPermission` | `DbPermission` | `permissions` | `/tenantId` | `UserId` | `ICreatable, IUpdatable, ISoftDeletable` | Change `DbRoleAssignment` from `class` → `sealed record` |

**Actual entity additions vs original spec** (user enriched the model from caropticom patterns):
- `DbUser` — richer naming; adds `AccountEnabled`, `AccountDisabledAt`, `AccountDisabledBy`, `ConnectedAt`, `DefaultTenantId`
- `DbRoleAssignment` — adds `ValidFrom`/`ValidTo` for temporal role validity
- `DbTenantAssignment` — nested in `DbUser`; implements `ICreatable` + `ISoftDeletable`
- `DbRole` — adds `Group` field (nullable)
- `TenantType` enum — added in `Users.Shared` with values `Node`/`Leaf`

**Folder layout inconsistency** — needs to be made uniform (all under `Models/` or all without):
```
Current:
  DbActions/DbAction.cs        ← not under Models/
  DbPermissions/DbPermission.cs ← not under Models/
  DbRoles/DbRole.cs             ← not under Models/
  Models/DbTenants/DbTenant.cs  ← under Models/
  Models/DbUsers/DbUser.cs ← under Models/

Target (all under Models/):
  Models/DbActions/DbAction.cs
  Models/DbPermissions/DbPermission.cs
  Models/DbRoles/DbRole.cs
  Models/DbTenants/DbTenant.cs
  Models/DbUsers/DbUser.cs
```

**New: `CosmosDocument` base record** in `Users.Infrastructure.Entities/Models/CosmosDocument.cs` — provides `_etag` and `_ts` for optimistic concurrency. All entities inherit it.

**Partition key rationale for `users` container:**
- Use `/email` not `/id` — users are most often looked up by email (login flow)
- Email is high-cardinality, unique per user, so each partition holds exactly one document
- This makes `GetByEmail` a single-partition point read (cheapest CosmosDB operation)

### 3. Tenant Hierarchy Design Decision

**Decision: Keep both `parentId` + `childIds`** — accepted by the team, knowing dual-write risk.

- Traversal in both directions available as point reads (no cross-partition query needed)
- **Dual-write contract**: any operation establishing or breaking a parent-child relationship MUST update both the child's `parentId` AND the parent's `childIds` array
- `ITenantRepository` exposes `AddChildAsync(parentId, childId)` and `RemoveChildAsync(parentId, childId)` helpers that encapsulate both writes
- Compensating logic: on partial failure, the repository method retries idempotently using ETag-based optimistic concurrency; callers must handle `CosmosException` (StatusCode 412 = precondition failed on ETag mismatch)
- Index policy: add `/parentId/?` index to support `GetChildrenAsync` fallback query for consistency checks

Diagram: Mermaid tree diagram showing bidirectional hierarchy navigation.

### 4. Permission Model (RBAC)

Explain the relationship:

```
User → [TenantAssignments] → Tenant
User + Tenant → DbPermission → [RoleAssignments] → Role → [ActionIds] → Action
```

- A user can belong to multiple tenants (`DbUser.TenantAssignments`)
- For each user-tenant combination, one `DbPermission` document exists
- A `DbPermission` holds a list of role IDs assigned to that user in that tenant
- Each `DbRole` holds a list of `ActionId`s defining what actions the role permits
- `DbRole.TenantId = Guid.Empty` means the role is global/system-level (available to all tenants)

Diagrams needed:
- Entity relationship diagram (Mermaid ER)
- Permission resolution flow (Mermaid sequence diagram)
- Tenant hierarchy example (Mermaid graph)

### 5. CosmosDB Container Design

For each container document:
- Container name, partition key, `id` field
- Expected access patterns with cross-partition warnings
- Index policy (composite indexes, excluded paths)

### 6. Repository Contracts

Show all interfaces with method signatures. Key design choices:

- Generic base: `IRepository<TEntity, TKey>` for CRUD
- `IRoleRepository` and `IPermissionRepository` do NOT extend the generic (composite partition keys)
- Cross-partition queries are annotated with `// WARNING: cross-partition fan-out` comments on the interface

### 7. CosmosDb Implementation

The implementation follows the pattern from `Libraries.Shared.CSharp` in the caropticom project.

**Core abstractions** (in `Users.Infrastructure.CosmosDb/Configuration/`):

```csharp
// Typed container provider — each T gets its own ICosmosDbContainerProvider<T> in DI
public interface ICosmosDbContainerProvider<T>
{
    Task<Container> GetContainerAsync();
}

// Typed key provider — resolves partition key + primary key per entity
public interface ICosmosDbKeysProvider<T>
{
    PartitionKey GetPartitionKey(T obj);
    string GetPrimaryKey(T obj);
}
```

**`SoftDeleteCosmosRepository<T>`** (base class for all repositories) provides:
- `AddAsync` / `AddMultipleAsync` (resets DeletedAt/DeletedBy before insert)
- `UpdateAsync` / `UpdateMultipleAsync`
- `GetAsync` (excludes soft-deleted) / `GetIncludingDeletedAsync`
- `GetAllAsync` (optional dynamic filter expression via `System.Linq.Dynamic.Core`)
- `GetMultipleAsync` (by ID or by batch of `(id, partitionKey)` tuples using `ReadManyItemsAsync`)
- `ExistsAsync` (batch existence check, excludes deleted)
- `DeleteAsync` / `DeleteMultipleAsync` (soft delete — sets `deletedAt` + `deletedBy`)
- `ExecuteTransactionsAsync` (groups items by partition key → transactional batch per partition)
- Protected `ExecuteQueryAsync<TValue>` helper
- Protected `ExecuteQueryWithParametersAsync` helper (auto-appends `deletedAt IS NULL` filter)

**`ICurrentDateTimeService`** — added to `Libraries.Shared/Services/`.

**Concrete repositories** extend `SoftDeleteCosmosRepository<T>` and add entity-specific:
- Custom queries (e.g., `GetByEmailAsync`, `GetByTenantIdAsync` with nested EXISTS)
- **Patch operations** using `container.PatchItemAsync` — critical for:
  - `PatchAddTenantAssignmentAsync` — appends to the `tenantAssignments` array without a full replace
  - `PatchRemoveTenantAssignmentAsync` — sets `deletedAt`/`deletedBy` on a specific array element

**DI setup** — fluent `CosmosDbConfigurator` pattern:
```csharp
// In Users.Infrastructure.CosmosDb/Extensions/ServiceCollectionExtensions.cs
services.AddUsersCosmosDb(configuration);

// After builder.Build():
await app.UseUsersCosmosDbAsync();   // creates DB + containers if not exist
await app.ApplyUsersMigrationsAsync();  // runs pending migrations
```

This registers one `ICosmosDbContainerProvider<T>` and one `ICosmosDbKeysProvider<T>` per entity type.

**Serialization** — Newtonsoft.Json serializer required (entities use `[JsonProperty]` attributes); configure `CosmosClientOptions.Serializer`.

**Authentication** — `DefaultAzureCredential` (Managed Identity) in Azure; `ConnectionString` for local emulator; `IgnoreSslCertificateValidation` option for local emulator TLS.

**Two-phase CosmosDB initialization** (critical — mirrors caropticom pattern):
- **Build phase** (`Program.cs` or `DiCompositor.ConfigureServices`): `services.AddUsersCosmosDb(configuration)` — registers `CosmosClient` singleton and all `ICosmosDbContainerProvider<T>` / `ICosmosDbKeysProvider<T>` per entity. Does NOT create DB/containers.
- **Run phase** (after `app = builder.Build()`): `await app.UseUsersCosmosDbAsync()` — runs `CosmosDbConfigurator` to create DB + containers if they don't exist. The app is ready to serve requests only after this completes.

**Options record pattern** — every options class must follow this exact shape:
```csharp
public sealed record CosmosDbOptions
{
    public const string ConfigSectionName = "Users:CosmosDb";

    [Required] public required string DatabaseName    { get; init; }

    // ConnectionString overrides AccountEndpoint for local emulator — optional, no [Required]
    public string? ConnectionString              { get; init; }
    public string? AccountEndpoint               { get; init; }
    public bool    IgnoreSslCertificateValidation { get; init; }
}
```

Rules:
- `sealed record` — immutable after construction
- `const string ConfigSectionName` — canonical section name, used in both DI and appsettings
- `[Required]` + `required` keyword on every mandatory field — `[Required]` triggers Data Annotation validation, `required` enforces it at compile time
- Optional fields: no `[Required]`, no `required`, use nullable type or a safe default
- `init`-only setters on all properties — no mutation after construction

**`ServiceOptionsExtensions.AddOptionsAndValidateOnStart<T>`** — called at DI setup for every options class:
```csharp
services.AddOptionsAndValidateOnStart<CosmosDbOptions>(configuration, CosmosDbOptions.ConfigSectionName);
```
Combines `.Bind()` + `.ValidateDataAnnotations()` + `.ValidateOnStart()`. App fails immediately at startup with a clear error if a `[Required]` field is missing from configuration.

### 8. Migration Mechanism

The migration system tracks applied schema/data changes in a `migrations` CosmosDB container. Each migration runs exactly once; applied versions are persisted so re-runs skip them.

#### `DbMigration` fix required
`src/Users.Infrastructure.Entities/Models/DbMigrations/DbMigration.cs` was `internal sealed record` — changed to `public sealed record` so the CosmosDb project can use it.

```csharp
// Container: migrations | Partition key: /id | Primary key: Id
public sealed record DbMigration
{
    [JsonProperty("id")]      public required string Id      { get; set; }
    [JsonProperty("version")] public required string Version { get; set; }
}
```

#### Where migration interfaces and implementations live

`IMigration` is **public** so that `Users.Infrastructure.CosmosDb.Migrations` (and any future migration assembly) can implement it. `IMigrationService` remains **internal** — exposed to consumers only via `ApplyUsersMigrationsAsync`.

```
Users.Infrastructure.CosmosDb/
  Migrations/
    IMigration.cs                        ← public interface (implementable cross-assembly)
    IMigrationService.cs                 ← internal interface
    MigrationService.cs                  ← internal sealed class (IEnumerable<IMigration> via DI)

Users.Infrastructure.CosmosDb.Migrations/   ← separate project
  V20250501_202100_InitialSeed.cs            ← first concrete migration
  DependencyInjection.cs                     ← AddUsersCosmosDbMigrations()
```

#### Version naming convention

```
YYYYMMDD_HHMMSS_Description
```

Example: `20250501_202100_InitialSeed`

Always use the **UTC timestamp** of when the migration was created. Lexicographic sort on the version string guarantees chronological application order.

#### `IMigration` interface
```csharp
// public — cross-assembly implementable
public interface IMigration
{
    string Version { get; }
    Task UpAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
```

`UpAsync` receives `IServiceProvider` so each migration resolves the repositories it needs.

#### `IMigrationService` interface

```csharp
internal interface IMigrationService
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken);
}
```

#### `MigrationService` implementation

`MigrationService` uses constructor injection — all `IMigration` singletons registered in DI are passed as `IEnumerable<IMigration>`:

```csharp
internal sealed class MigrationService(
    IServiceProvider serviceProvider,
    IEnumerable<IMigration> migrations,
    IMigrationRepository migrationRepository,
    ILogger<MigrationService> logger)
    : IMigrationService
{
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var applied = await migrationRepository.GetAllAsync(cancellationToken);
        var pending = migrations
            .Where(m => applied.All(a => a.Version != m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0) { logger.LogInformation("No pending migrations"); return; }

        logger.LogInformation("{Count} migration(s) pending", pending.Count);
        foreach (var migration in pending)
        {
            logger.LogInformation("Applying migration {Version}", migration.Version);
            await migration.UpAsync(serviceProvider, cancellationToken);
            await migrationRepository.AddAsync(
                new DbMigration { Id = Guid.NewGuid().ToString(), Version = migration.Version },
                cancellationToken);
            logger.LogInformation("Migration {Version} applied", migration.Version);
        }
    }
}
```

#### `IMigrationRepository` (in `Users.Infrastructure.Contracts`)

```csharp
public interface IMigrationRepository
{
    Task<IReadOnlyList<DbMigration>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(DbMigration migration, CancellationToken cancellationToken);
}
```

- Lives in `Users.Infrastructure.Contracts/Repositories/IMigrationRepository.cs`
- Implemented by `MigrationRepository` in `Users.Infrastructure.CosmosDb/Repositories/`
- `migrations` container: partition key `/id`, no soft-delete (migrations are permanent)

#### Public entry point

`IMigrationService` is internal. Consumers call the public extension method:

```csharp
// In Users.InitContainer/Program.cs — after UseUsersCosmosDbAsync
await app.ApplyUsersMigrationsAsync(CancellationToken.None);
```

#### DI registration

Migrations are registered **separately** from the infrastructure, in `Users.Infrastructure.CosmosDb.Migrations/DependencyInjection.cs`:

```csharp
// Users.Infrastructure.CosmosDb.Migrations/DependencyInjection.cs
public static IServiceCollection AddUsersCosmosDbMigrations(this IServiceCollection services)
{
    services.AddSingleton<IMigration, V20250501_202100_InitialSeed>();
    return services;
}
```

`MigrationService` receives all registered `IMigration` singletons via `IEnumerable<IMigration>` constructor injection. Adding a new migration = add one `AddSingleton<IMigration, ...>` call.

#### First migration: `V20250501_202100_InitialSeed`

Creates the bootstrap data that every new deployment needs. All well-known IDs come from `Root` in `Users.Authorization.Constants`:

```csharp
public static class Root
{
    public static readonly Guid   SystemId         = Guid.Empty;
    public static readonly string TenantId         = "9d6e73c7-c5e0-4a6b-a23d-1a307cdcf30c";
    public static readonly string SuperAdminRoleId = "a50fcd0f-e253-41eb-98b3-ef02bbde476e";
}
```

**Root tenant:**
```
TenantId  = Root.TenantId  ("9d6e73c7-...")
Name      = "Root"
TenantType = Node
ParentId  = Root.SystemId.ToString()   ← "00000000-..." = no parent
```

**`Users.Authorization.Constants` project** — action IDs are split into three focused classes:

```csharp
// Actions/TenantActions.cs
public static class TenantActions { public const string TenantsView = "Tenants.View"; }

// Actions/UserActions.cs
public static class UserActions
{
    public const string ModuleUsers = "Module.Users";
    public const string UsersView = "Users.View";
    // ... 10 more
}

// Actions/AuthActions.cs
public static class AuthActions
{
    public const string AuthGetRoles = "Auth.GetRoles";
    public const string AuthGetRolesAllTenants = "Auth.GetRoles.AllTenants";
    public const string AuthGetActions = "Auth.GetActions";
}
```

Also contains `Root.cs` (system constants) and `UsersCosmosDbConstants.cs` (container names + partition key paths as typed `ContainerOptions` records).

`Users.Authorization.Constants` has no project references; it is referenced by `Users.Infrastructure.CosmosDb.Migrations` (for migration constants) and later by `HttpApi` / `FunctionApp1` (for authorization policy definitions).

**Initial actions** (ActionId = the constant string value; document `id` = action string):

| ActionId | Name |
|---|---|
| `Tenants.View` | View tenant data |
| `Module.Users` | Access Users Section |
| `Users.View` | View users in own tenant |
| `Users.View.AllTenants` | View users in all tenants |
| `Users.Add` | Add user to own tenant |
| `Users.Add.AllTenants` | Add user to any tenant |
| `Users.Edit` | Edit user in own tenant |
| `Users.Edit.AllTenants` | Edit user in any tenant |
| `Users.AssignExplicitActions` | Assign explicit actions to user |
| `Users.AssignRoles` | Assign roles to user |
| `Users.Delete` | Delete user in own tenant |
| `Users.Delete.AllTenants` | Delete user in any tenant |
| `Users.EditOwnProfile` | Edit own profile |
| `Auth.GetRoles` | Get list of all roles in own tenant |
| `Auth.GetRoles.AllTenants` | Get list of roles in any tenant |
| `Auth.GetActions` | Get list of all actions in the system |

**Initial global role** (global = `TenantId = Guid.Empty`; fixed GUID for reproducibility):

```json
{
  "id": "a50fcd0f-e253-41eb-98b3-ef02bbde476e",
  "name": "Super Admin",
  "tenantId": "00000000-0000-0000-0000-000000000000",
  "actionIds": ["Tenants.View", "Module.Users", "Users.View", "Users.View.AllTenants",
                "Users.Add", "Users.Add.AllTenants", "Users.Edit", "Users.Edit.AllTenants",
                "Users.AssignExplicitActions", "Users.AssignRoles", "Users.Delete",
                "Users.Delete.AllTenants", "Users.EditOwnProfile", "Auth.GetRoles",
                "Auth.GetRoles.AllTenants", "Auth.GetActions"]
}
```

The migration resolves `IActionRepository`, `IRoleRepository`, and `ITenantRepository` from `IServiceProvider` and calls `AddAsync` on each. All three are idempotent — existing documents are skipped.

#### `Users.InitContainer` calls migrations

In `Program.cs`, after `await app.UseUsersCosmosDbAsync()` creates the database + containers:

```csharp
await app.ApplyUsersMigrationsAsync(CancellationToken.None);
```

This runs before the JSON seeders so roles created by migrations can be referenced in `permissions.json`.

### 9. DiCompositor Pattern

Each consuming project (`HttpApi`, `FunctionApp1`) must have a `DiCompositor.cs` (or `Extensions.cs`) that moves DI registration out of `Program.cs`:

```
HttpApi/
  DiCompositor.cs   ← new file
  Program.cs        ← becomes: builder.ConfigureInstrumentation(); builder.Services.ConfigureServices(builder.Configuration); await app.UseUsersCosmosDbAsync();
```

`DiCompositor.cs` structure:
```csharp
public static class DiCompositor
{
    public static void ConfigureInstrumentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<HttpApiInstrumentation>();
    }

    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsAndValidateOnStart<CosmosDbOptions>(configuration, CosmosDbOptions.ConfigSectionName);
        services.AddUsersCosmosDb(configuration);
        // other domain registrations...
    }
}
```

This keeps `Program.cs` to a minimal bootstrap sequence and makes domain registrations composable and testable.

### 10. InitContainer Design

**`Users.InitContainer/Program.cs`** — 4-phase initialization sequence:
1. **`UseUsersCosmosDbAsync`** — creates CosmosDB database + all containers if they don't exist
2. **`ApplyUsersMigrationsAsync`** — schema/data migrations for version-to-version upgrades
3. **SeedTenants** — reads `tenants.json`, builds dependency tree, processes parent-before-child, handles duplicates
4. **SeedUsers** — discovers user directories, validates `user.json` + `permissions.json`, creates users + permissions

**`SeederOptions`** (validated via `AddOptionsAndValidateOnStart`):
```csharp
public sealed record SeederOptions
{
    public const string ConfigSectionName = "Seeder";
    [Required] public required bool   TenantsSeed     { get; init; }
    [Required] public required bool   UsersSeed        { get; init; }
    [Required] public required string SeedDataFilePath { get; init; }
}
```

**`UsersInitContainerInstrumentation`** — same `ActivitySource` pattern as `HttpApiInstrumentation`; add to `Diagnostics/` folder.

**Seed data layout** — files on filesystem (NOT embedded resources), path injected via `SeederOptions.SeedDataFilePath`:
```
{SeedDataFilePath}/
  users-db/
    tenants.json         ← flat array; seeder builds tree, processes parent-before-child
    users/
      {user@email.com}/
        user.json          ← DbUser document
        permissions.json   ← REQUIRED; array of { tenantId, roleAssignments[] } per tenant
```

- Per-email directory structure = each user is independently versioned in git
- `permissions.json` is required for every user; missing file → exception during seed
- Role assignments support `roleId` (direct GUID) or `roleName` (seeder resolves to ID)
- `CopyToOutputDirectory: PreserveNewest` on all JSON files in the csproj
- Volume mount in Docker Compose: `${SEED_DATA_PATH}:/app/seed-data`

Exit code 0 = success, non-zero = failure (Azure Container Apps Job lifecycle depends on this).

### 11. Local Development (Docker Compose)

- New `docker-compose.cosmosdb.yaml` file (follows existing `docker-compose.observability.yaml` pattern)
- CosmosDB Linux emulator: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-EN20251223`
- `users-init-container` service depends on emulator health check (mirrors azurite-init pattern)
- For Parallels Desktop emulator: use `localhost:8081` with Windows emulator account key, `IgnoreSslCertificateValidation: false`
- For Docker Linux emulator: use container hostname with Linux emulator account key, `IgnoreSslCertificateValidation: true`
- `.env.dev` additions: `USERS_INIT_CONTAINER_IMAGE`, `USERS_COSMOSDB_CONNECTION_STRING`, `USERS_COSMOSDB_DATABASE`, `SEED_DATA_PATH`

### 12. Azure Deployment (Bicep)

New modules to add:

| Module | Creates |
|---|---|
| `infrastructure/modules/cosmosdb.bicep` | CosmosDB account + `users-db` database (serverless for dev, provisioned for prod) |
| `infrastructure/modules/helpers/cosmosdb-role-assignment.bicep` | Grants user-assigned identity the `Cosmos DB Built-in Data Contributor` role |
| `infrastructure/modules/helpers/init-container-job.bicep` | Container Apps Job for `Users.InitContainer` (Manual trigger, deployed by pipeline) |

Wire all three into `infrastructure/main.bicep` with `dependsOn` ordering, behind an `enableUsersModule` flag.

---

## Phase 2 Output: Project Scaffold (After docs/users.md Approval)

### Disk Layout (as-built)

```
src/
  Users.Authorization.Constants/
    Users.Authorization.Constants.csproj   ← no project refs, no NuGet deps
    Root.cs                                ← SystemId, TenantId, SuperAdminRoleId constants
    UsersCosmosDbConstants.cs              ← container names + partition key paths as ContainerOptions records
    Actions/
      TenantActions.cs                     ← TenantsView
      UserActions.cs                       ← 13 user-management action IDs
      AuthActions.cs                       ← AuthGetRoles, AuthGetRolesAllTenants, AuthGetActions

  Users.Infrastructure.Entities/
    Users.Infrastructure.Entities.csproj
    Models/
      DbUsers/DbUser.cs
      DbTenants/DbTenant.cs
      DbRoles/DbRole.cs
      DbActions/DbAction.cs               ← public; no soft-delete
      DbPermissions/DbPermission.cs
      DbPermissions/DbRoleAssignment.cs   ← sealed record; ValidFrom/ValidTo
      DbMigrations/DbMigration.cs         ← public sealed record

  Users.Infrastructure.Contracts/
    Users.Infrastructure.Contracts.csproj
    Repositories/
      IRepository.cs
      IUserRepository.cs
      ITenantRepository.cs
      IRoleRepository.cs
      IActionRepository.cs
      IPermissionRepository.cs
      IMigrationRepository.cs
      IUsersCosmosDbManagerRepository.cs  ← CreateDatabaseIfNotExistsAsync / DropDatabaseIfExistsAsync

  Libraries.Shared.CosmosDb/
    Libraries.Shared.CosmosDb.csproj
    Configuration/
      ICosmosDbContainerProvider.cs       ← generic typed container handle
      ICosmosDbKeysProvider.cs            ← partition key + primary key selectors
      CosmosDbConfigurator.cs             ← multi-database registry (CosmosDbReference keyed dict)
      CosmosDbClientProvider.cs           ← creates CosmosClient per database
      CosmosDbContainerProvider.cs        ← Lazy<Task<Container>>
      CosmosDbContainerBuilder.cs         ← fluent per-container configuration
      CosmosDbContainerOptions.cs         ← per-container name + key selectors
      CosmosDbDatabaseOptions.cs          ← connection, throughput, TLS, container builder
      CosmosDbKeysProvider.cs
      CosmosDbReference.cs                ← (ConnectionString, DatabaseId) equality key
      ICosmosDbContainerOptions.cs
    ServicesExtensions.cs                 ← AddCosmosDb()

  Users.Infrastructure.CosmosDb/
    Users.Infrastructure.CosmosDb.csproj
    Options/
      UsersInfrastructureCosmosDbOptions.cs  ← ConnectionString, DatabaseId, Throughput, UseIntegratedCache, TLS
    Repositories/
      SoftDeleteCosmosRepository.cs       ← abstract base for ISoftDeletable entities
      UserRepository.cs
      TenantRepository.cs
      RoleRepository.cs
      ActionRepository.cs
      PermissionRepository.cs
      MigrationRepository.cs
      UsersCosmosDbManagerRepository.cs   ← implements IUsersCosmosDbManagerRepository
    Migrations/
      IMigration.cs                       ← public interface
      IMigrationService.cs                ← internal interface
      MigrationService.cs                 ← internal sealed; IEnumerable<IMigration> via DI
    CosmosDbExtensions.cs                 ← UseUsersCosmosDb(IHost) + ConfigureUsersCosmosDb(IServiceProvider)
    Extensions/
      HostExtensions.cs                   ← UseUsersCosmosDbAsync / ApplyUsersMigrationsAsync
    DependencyInjection.cs                ← AddUsersCosmosDb(IConfiguration)

  Users.Infrastructure.CosmosDb.Migrations/
    Users.Infrastructure.CosmosDb.Migrations.csproj
    V20250501_202100_InitialSeed.cs       ← actions + Super Admin role + Root tenant
    DependencyInjection.cs               ← AddUsersCosmosDbMigrations()

  Users.InitContainer/
    Users.InitContainer.csproj           ← OutputType=Exe
    Program.cs                           ← 3-phase: DB create → migrations → seeders
    Dockerfile
    entrypoint.sh
    Diagnostics/
      UsersInitContainerInstrumentation.cs
    appsettings.json
    appsettings.dev.json

  Users.InitContainer.Data/
    Users.InitContainer.Data.csproj
    Options/
      SeederOptions.cs                   ← ConfigSectionName = "SeederOptions"
    Models/
      SeedPermission.cs
      SeedRoleAssignment.cs
    Seeders/
      TenantSeeder.cs
      UserSeeder.cs
    DependencyInjection.cs               ← AddUsersInitContainerData()
    SeedData/
      users-db/
        tenants.json
        users/
          {user@email.com}/
            user.json
            permissions.json             ← REQUIRED per user
```

### Project Dependency Graph (as-built)

```
Users.Authorization.Constants          ← zero deps; action IDs, Root, UsersCosmosDbConstants

Libraries.Shared                       ← ICurrentDateTimeService + ServiceOptionsExtensions
  └── Libraries.Shared.CosmosDb        ← AddCosmosDb(); CosmosDbConfigurator; typed providers
  └── Users.Shared                     ← TenantType enum
        └── Users.Infrastructure.Entities
              └── Users.Infrastructure.Contracts
                    └── Users.Infrastructure.CosmosDb          [+ Microsoft.Azure.Cosmos
                    │     + Users.Authorization.Constants        + Newtonsoft.Json
                    │     + Libraries.Shared.CosmosDb            + Azure.Identity
                    │                                            + System.Linq.Dynamic.Core]
                    │         └── Users.Infrastructure.CosmosDb.Migrations
                    │               └── Users.InitContainer    [+ Users.InitContainer.Data]
                    └── (HttpApi / FunctionApp1 call AddUsersCosmosDb + AddUsersCosmosDbMigrations)
```

### Key csproj Details

- All projects: `net8.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
- `Entities`: `Newtonsoft.Json 13.0.4` for `[JsonProperty]`
- `CosmosDb`: `Microsoft.Azure.Cosmos 3.46.0`, `Azure.Identity 1.13.2`, `Newtonsoft.Json 13.0.4`, `System.Linq.Dynamic.Core 1.6.0`, `Microsoft.Extensions.Hosting.Abstractions 8.0.1`, `Microsoft.Extensions.Logging.Abstractions 8.0.3`
- `InitContainer`: `<OutputType>Exe</OutputType>`, `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3`

---

## Claude Code Skills

Four skills implemented as `.claude/commands/*.md` files (invokable via `/skill-name` in Claude Code CLI):

| Command | File | Purpose |
|---|---|---|
| `/scaffold-cosmos-repository` | `.claude/commands/scaffold-cosmos-repository.md` | Generate `{Entity}Repository.cs` from template + wire DI in `ServiceCollectionExtensions.cs` |
| `/run-users-init-container` | `.claude/commands/run-users-init-container.md` | Check emulator health, build image, run init container locally, tail output |
| `/add-seed-data` | `.claude/commands/add-seed-data.md` | Prompt for fields, validate shape, append to the right `SeedData/*.json` |
| `/check-cosmos-container` | `.claude/commands/check-cosmos-container.md` | Query local emulator, show item counts + index policies per container |

---

## Provider Independence (Swapping CosmosDB for Entity Framework or Any Other Backend)

This is a **core architectural guarantee** of the module structure. No consumer ever depends on a specific data provider.

### How it works

```
                     ┌─────────────────────────┐
                     │  Users.Infrastructure   │
                     │      .Contracts         │
                     │  (repository interfaces)│
                     └────────────┬────────────┘
                                  │ implements
              ┌───────────────────┼──────────────────────┐
              ▼                                           ▼
┌──────────────────────────┐             ┌───────────────────────────────┐
│ Users.Infrastructure     │             │ Users.Infrastructure          │
│ .CosmosDb                │             │ .EntityFramework  (new later) │
│                          │             │                               │
│ UserRepository           │             │ EfUserRepository              │
│ TenantRepository         │             │ EfTenantRepository            │
│ ...                      │             │ ...                           │
└──────────────────────────┘             └───────────────────────────────┘
              │                                           │
              └──────────────┬────────────────────────────┘
                             │  only one registered at a time
                             ▼
              ┌────────────────────────────┐
              │  HttpApi / FunctionApp1    │
              │  Program.cs               │
              │                           │
              │  // swap this one line:   │
              │  .AddUsersCosmosDb(...)   │
              │  // or:                   │
              │  .AddUsersEntityFramework(│
              │    builder.Configuration) │
              └────────────────────────────┘
```

### To switch from CosmosDB to Entity Framework (or any backend)

1. **Create new project** `Users.Infrastructure.EntityFramework`
   - References `Users.Infrastructure.Contracts` and `Users.Infrastructure.Entities`
   - Adds `Microsoft.EntityFrameworkCore` (and the target provider)
   - Implements every interface from `Users.Infrastructure.Contracts/Repositories/`

2. **Implement all interfaces** (`EfUserRepository : IUserRepository`, etc.)

3. **Add DI extension** `AddUsersEntityFramework(IConfiguration)` in the new project

4. **In the consuming project** change **one line**:
   ```csharp
   // Before:
   builder.Services.AddUsersCosmosDb(builder.Configuration);
   // After:
   builder.Services.AddUsersEntityFramework(builder.Configuration);
   ```

5. **Remove the reference** to `Users.Infrastructure.CosmosDb`; add reference to `Users.Infrastructure.EntityFramework`

**Nothing else changes** — `HttpApi`, `FunctionApp1`, `Users.Infrastructure.Contracts`, and `Users.Infrastructure.Entities` are completely untouched.

### Rules enforced by project structure

| Rule | How enforced |
|---|---|
| Consumers never reference CosmosDB types directly | `HttpApi.csproj` references `Contracts` (interfaces) + `CosmosDb` (for DI registration only) |
| No CosmosDB SDK bleeds into entity layer | `Entities.csproj` has no reference to `Microsoft.Azure.Cosmos` |
| No CosmosDB SDK bleeds into contracts layer | `Contracts.csproj` has no reference to `Microsoft.Azure.Cosmos` |
| `ICosmosDbContainerProvider<T>`, `ICosmosDbKeysProvider<T>`, `SoftDeleteCosmosRepository<T>` are CosmosDB-specific | These live in `Users.Infrastructure.CosmosDb/`, never in `Contracts` |
| Swapping is a 1-project reference change | Remove `CosmosDb` project reference, add `EntityFramework` project reference; call `AddUsersEntityFramework()` instead |

---

## Critical Files

- `src/Libraries.Shared/Interfaces/` — base interfaces that entities must implement exactly
- `docker-compose.yaml` — azurite+azurite-init pattern replicated for cosmosdb+users-init-container
- `infrastructure/main.bicep` — where new Bicep modules plug in
- `infrastructure/modules/helpers/azure-function-container-app-helper.bicep` — role assignment pattern replicated

**Reference implementations from caropticom project (do not modify, use as template):**
- `caropticom/Libraries.Shared.CSharp/CosmosDb/Repositories/SoftDeleteCosmosRepository.cs` — base repository
- `caropticom/Libraries.Shared.CSharp/CosmosDb/Configuration/ICosmosDbContainerProvider.cs` — typed container provider interface
- `caropticom/Libraries.Shared.CSharp/Helpers/ServiceOptionsExtensions.cs` — options validation helper
- `caropticom/Users.Infrastructure.AuthorizationStorage/CosmosDbExtensions.cs` — fluent configurator pattern
- `caropticom/Users.InitContainer/Program.cs` — init container structure
- `caropticom/Users.InitContainer.Data/Repositories/UserSeederRepository.cs` — user seeder pattern

---

## Verification

1. ✅ `dotnet build` passes for all 9 Users projects (0 errors) — verified 2026-05-01
2. `docker compose -f docker-compose.yaml -f docker-compose.cosmosdb.yaml --env-file .env.dev up` starts emulator healthy
3. `users-init-container` service exits with code 0
4. Emulator explorer at `https://localhost:8081/_explorer/index.html` shows `users-db` with 6 containers (`users`, `tenants`, `roles`, `actions`, `permissions`, `migrations`) and migration seed data
5. Bicep: `az deployment sub what-if` shows all three new resources without errors
