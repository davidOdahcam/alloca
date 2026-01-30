using Alloca.Application;
using Alloca.Application.Common.Interfaces;
using Alloca.IoC.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class ApplicationConfig
{
    public static IServiceCollection AddApplicationConfig(this IServiceCollection services)
    {
        services.AddApplication();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }
}
