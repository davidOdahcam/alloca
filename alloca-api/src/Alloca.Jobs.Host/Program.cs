using Alloca.Application;
using Alloca.Infra;
using Alloca.Infra.Persistence;
using Alloca.Jobs;
using Alloca.Jobs.Host;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Camadas reutilizadas da API
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Jobs do domínio
builder.Services.AddAllocaJobs();

// Hangfire (storage + servidor)
var conn = builder.Configuration.GetConnectionString("AllocaDb")
    ?? throw new InvalidOperationException("Connection string 'AllocaDb' não configurada.");

builder.Services.AddHangfire(cfg => cfg
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

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"alloca-jobs-{Environment.MachineName}";
    options.WorkerCount = Math.Max(2, Environment.ProcessorCount);
});

// Garante migrations aplicadas e agenda os jobs recorrentes no startup
builder.Services.AddHostedService<JobsBootstrapHostedService>();

var host = builder.Build();
host.Run();
