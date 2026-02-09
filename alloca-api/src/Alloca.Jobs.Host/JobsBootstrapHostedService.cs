using Alloca.Infra.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alloca.Jobs.Host;

/// <summary>
/// Garante que o banco esteja migrado e registra os jobs recorrentes no Hangfire
/// assim que o host de jobs sobe.
/// </summary>
internal sealed class JobsBootstrapHostedService(
    IServiceProvider services,
    IRecurringJobManager recurringJobs,
    ILogger<JobsBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AllocaDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
        }

        Alloca.Jobs.DependencyInjection.RegisterRecurringJobs(recurringJobs);
        logger.LogInformation("Jobs recorrentes registrados no Hangfire.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
