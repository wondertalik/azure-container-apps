namespace Users.Infrastructure.CosmosDb.Migrations;

public interface IMigration
{
    string Version { get; }

    Task UpAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
