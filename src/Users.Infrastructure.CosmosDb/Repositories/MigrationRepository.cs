using Microsoft.Azure.Cosmos;
using Libraries.Shared.CosmosDb.Configuration;
using Users.Infrastructure.Contracts.Repositories;
using Users.Infrastructure.Entities.Models.DbMigrations;

namespace Users.Infrastructure.CosmosDb.Repositories;

internal sealed class MigrationRepository(ICosmosDbContainerProvider<DbMigration> containerProvider)
    : IMigrationRepository
{
    public async Task<IReadOnlyList<DbMigration>> GetAllAsync(CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        var query = new QueryDefinition($"SELECT * FROM {container.Id} c");
        var result = new List<DbMigration>();
        using var iterator = container.GetItemQueryIterator<DbMigration>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            result.AddRange(page);
        }

        return result;
    }

    public async Task AddAsync(DbMigration migration, CancellationToken cancellationToken)
    {
        Container container = await containerProvider.GetContainerAsync();
        await container.CreateItemAsync(migration, new PartitionKey(migration.Id),
            cancellationToken: cancellationToken);
    }
}
