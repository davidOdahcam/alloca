using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

public static class HangfireConfig
{
    /// <summary>
    /// Registra apenas o cliente do Hangfire (storage + APIs para enfileirar/agendar/monitorar).
    /// Use na API. O servidor que processa os jobs roda no projeto Alloca.Jobs.Host.
    /// </summary>
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

        return services;
    }
}
