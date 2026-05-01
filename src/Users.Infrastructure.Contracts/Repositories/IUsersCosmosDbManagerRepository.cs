namespace Users.Infrastructure.Contracts.Repositories;

/// <summary>
/// Manages the Users CosmosDB database lifecycle (create, drop).
/// </summary>
/// <remarks>
/// <see cref="CreateDatabaseIfNotExistsAsync"/> relies on the CosmosDB configurator being
/// populated first. Ensure <c>UseUsersCosmosDb()</c> is called before invoking this method.
/// </remarks>
public interface IUsersCosmosDbManagerRepository
{
    Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default);

    Task DropDatabaseIfExistsAsync(CancellationToken cancellationToken = default);
}
