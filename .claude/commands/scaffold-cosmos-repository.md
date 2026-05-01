Generate a CosmosDB repository implementation for a Users module entity.

Usage: /scaffold-cosmos-repository <EntityName>

Example: /scaffold-cosmos-repository DbUser

Steps:
1. Read the entity class from `src/Users.Infrastructure.Entities/Models/` to understand its properties, partition key, and primary key.
2. Read `src/Users.Infrastructure.Contracts/Repositories/` to find the matching interface (e.g. `IUserRepository` for `DbUser`).
3. Read `src/Users.Infrastructure.CosmosDb/Repositories/` for an existing repository as a pattern reference.
4. Create `src/Users.Infrastructure.CosmosDb/Repositories/{Entity}Repository.cs`:
   - `internal sealed class {Entity}Repository(ICosmosDbContainerProvider<{Entity}> provider, ICosmosDbKeysProvider<{Entity}> keys, ICurrentDateTimeService clock) : SoftDeleteCosmosRepository<{Entity}>(provider, keys, clock), I{Entity}Repository`
   - Implement all interface methods not already provided by the base class
   - Use `ExecuteQueryAsync` for custom queries
   - Use `container.PatchItemAsync` for partial updates (array manipulation, field patches)
   - Add `// WARNING: cross-partition fan-out` comments on cross-partition queries
5. Register the repository in `src/Users.Infrastructure.CosmosDb/Extensions/ServiceCollectionExtensions.cs`:
   - `services.AddScoped<I{Entity}Repository, {Entity}Repository>();`
   - Add `.Configure<{Entity}>(opts => opts.WithName("...").WithPartitionKeyPath("...").WithPrimaryKey(...).WithPartitionKey(...))` to the container builder
6. Run `dotnet build src/Users.Infrastructure.CosmosDb/Users.Infrastructure.CosmosDb.csproj` to verify.

Report what was created and any methods that need custom implementation.
