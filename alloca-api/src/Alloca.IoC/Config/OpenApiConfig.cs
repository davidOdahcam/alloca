using Alloca.IoC.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Alloca.IoC.Config;

public static class OpenApiConfig
{
    public static IServiceCollection AddOpenApiConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
        return services;
    }

    public static IApplicationBuilder UseOpenApiConfig(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opt =>
        {
            opt.WithTitle("Alloca API")
               .WithTheme(ScalarTheme.BluePlanet);
        });
        return app;
    }
}
