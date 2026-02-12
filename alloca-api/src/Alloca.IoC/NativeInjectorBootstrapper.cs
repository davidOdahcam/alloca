using System.Text.Json.Serialization;
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
        services.AddLocalizationConfig();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        return services;
    }
}
