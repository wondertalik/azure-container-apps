using FunctionApp1.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionApp1;

public static class DiCompositor
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        
    }
    
    public static void ConfigureInstrumentation(this IServiceCollection services)
    {
        services.AddSingleton<FunctionApp1Instrumentation>();
    }
}