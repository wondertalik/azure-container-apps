using Libraries.Shared.CosmosDb.Configuration;
using Libraries.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libraries.Shared.CosmosDb;

public static class ServicesExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection services)
    {
        services.TryAddSingleton<CosmosDbConfigurator>();
        services.TryAddSingleton<CosmosDbClientProvider>();
        services.TryAddSingleton(typeof(ICosmosDbContainerProvider<>), typeof(CosmosDbContainerProvider<>));
        services.TryAddSingleton(typeof(ICosmosDbKeysProvider<>), typeof(CosmosDbKeysProvider<>));
        services.TryAddSingleton<ICurrentDateTimeService, CurrentDateTimeService>();

        return services;
    }
}
