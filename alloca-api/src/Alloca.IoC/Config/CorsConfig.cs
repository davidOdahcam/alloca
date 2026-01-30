using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class CorsConfig
{
    public const string DefaultPolicy = "AllocaCors";

    public static IServiceCollection AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(o => o.AddPolicy(DefaultPolicy, p => p
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("http://localhost:4200", "https://localhost:4200")));
        return services;
    }
}
