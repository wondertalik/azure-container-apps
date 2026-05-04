using Libraries.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.InitContainer.Data.Options;
using Users.InitContainer.Data.Seeders;

namespace Users.InitContainer.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInitContainerData(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsAndValidateOnStart<SeederOptions>(configuration, SeederOptions.ConfigSectionName);

        services.AddTransient<TenantSeeder>();
        services.AddTransient<UserSeeder>();

        return services;
    }
}
