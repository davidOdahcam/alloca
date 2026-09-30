using System.Text.Json.Serialization;
using Alloca.IoC.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC;

public static class NativeInjectorBootStrapper
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationConfig();
        services.AddInfrastructureConfig(configuration);
        services.AddAuthConfig(configuration);
        services.AddOpenApiConfig();
        services.AddCorsConfig();
        services.AddLocalizationConfig();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
            });
        return services;
    }
}
