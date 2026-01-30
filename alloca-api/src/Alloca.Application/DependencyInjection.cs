using System.Reflection;
using Alloca.Application.Services;
using Alloca.Application.Services.Implementations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPavilionService, PavilionService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<IBlockService, BlockService>();

        return services;
    }
}
