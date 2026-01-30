using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Repositories;
using Alloca.Infra.Background;
using Alloca.Infra.Persistence;
using Alloca.Infra.Persistence.Repositories;
using Alloca.Infra.Persistence.Seed;
using Alloca.Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.Infra;

public static class DependencyInjection
{
    public const string DefaultConnectionName = "AllocaDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DefaultConnectionName}' not configured.");

        services.AddDbContext<AllocaDbContext>(opt =>
            opt.UseSqlServer(conn, sql => sql.MigrationsAssembly(typeof(AllocaDbContext).Assembly.FullName)));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<ReservationPolicySettings>(configuration.GetSection("ReservationPolicy"));

        // Domain services / utilities
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();

        // Unit of Work + Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPavilionRepository, PavilionRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IDeskRepository, DeskRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IUserStrikeRepository, UserStrikeRepository>();
        services.AddScoped<IUserSuspensionRepository, UserSuspensionRepository>();

        // Background + seed
        services.AddScoped<ReservationLifecycleJob>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
