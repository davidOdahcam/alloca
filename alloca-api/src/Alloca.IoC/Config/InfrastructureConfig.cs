using Alloca.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class InfrastructureConfig
{
    public static IServiceCollection AddInfrastructureConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        return services;
    }
}
