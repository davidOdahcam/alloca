using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class HangfireConfig
{
    public static IServiceCollection AddHangfireConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("AllocaDb")
            ?? throw new InvalidOperationException("Connection string 'AllocaDb' not configured.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(conn, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
        services.AddHangfireServer();
        return services;
    }
}
