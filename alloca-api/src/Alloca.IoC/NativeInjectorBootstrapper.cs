using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class NativeInjectorBootStrapper
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationConfig();
        services.AddInfrastructureConfig(configuration);
        services.AddAuthConfig(configuration);
        services.AddHangfireConfig(configuration);
        services.AddOpenApiConfig();
        services.AddCorsConfig();
        services.AddControllers();
        return services;
    }
}
