using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libraries.Shared.Helpers;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddOptionsAndValidateOnStart<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) where T : class
    {
        services
            .AddOptions<T>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddOptionsAndValidateOnStart<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        out T optionsValue) where T : class
    {
        AddOptionsAndValidateOnStart<T>(services, configuration, sectionName);
        optionsValue = configuration.GetRequiredSection(sectionName).Get<T>()!;
        return services;
    }
}
