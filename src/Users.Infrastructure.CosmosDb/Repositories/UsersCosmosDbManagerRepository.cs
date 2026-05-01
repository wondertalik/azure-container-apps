using System.Net;
using Libraries.Shared.CosmosDb.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.CosmosDb.Options;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class UsersCosmosDbManagerRepository(
    CosmosDbClientProvider clientProvider,
    CosmosDbConfigurator configurator,
    IOptions<UsersInfrastructureCosmosDbOptions> options,
    ILogger<UsersCosmosDbManagerRepository> logger)
    : IUsersCosmosDbManagerRepository
{
    public async Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var client = await clientProvider.GetCosmosClientAsync(opts.ConnectionString, opts.DatabaseId);

        logger.LogInformation("Creating database '{DatabaseId}' if not exists", opts.DatabaseId);

        var response = await client.CreateDatabaseIfNotExistsAsync(
            opts.DatabaseId, opts.Throughput, cancellationToken: cancellationToken);

        CosmosDbReference reference = new(opts.ConnectionString, opts.DatabaseId);
        if (!configurator.DatabasesConfig.TryGetValue(reference, out var dbOptions))
        {
            logger.LogWarning(
                "No CosmosDB configuration found for database '{DatabaseId}'. Containers not created.",
                opts.DatabaseId);
            return;
        }

        foreach (var containerOpts in dbOptions.ContainerBuilder.ContainersConfig.Values)
        {
            logger.LogInformation(
                "Creating container '{ContainerName}' if not exists", containerOpts.Name);

            await response.Database.CreateContainerIfNotExistsAsync(
                containerOpts.Name,
                containerOpts.PartitionKey,
                cancellationToken: cancellationToken);
        }
    }

    public async Task DropDatabaseIfExistsAsync(CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var client = await clientProvider.GetCosmosClientAsync(opts.ConnectionString, opts.DatabaseId);

        try
        {
            var db = client.GetDatabase(opts.DatabaseId);
            await db.ReadAsync(cancellationToken: cancellationToken);
            await db.DeleteAsync(cancellationToken: cancellationToken);

            logger.LogInformation("Database '{DatabaseId}' dropped", opts.DatabaseId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                "Database '{DatabaseId}' does not exist — nothing to drop", opts.DatabaseId);
        }
    }
}
