using Alloca.Jobs.Reservations;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.Jobs;

/// <summary>
/// Extensões para registrar e agendar os jobs do Alloca.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos os jobs no contêiner de DI (escopo por execução).
    /// Pode ser usado tanto pelo host de jobs quanto pela API
    /// (caso seja necessário agendar/enfileirar jobs sob demanda).
    /// </summary>
    public static IServiceCollection AddAllocaJobs(this IServiceCollection services)
    {
        services.AddScoped<ReservationLifecycleJob>();
        return services;
    }

    /// <summary>
    /// Registra os jobs recorrentes no Hangfire.
    /// Deve ser chamado apenas pelo processo que hospeda o servidor Hangfire.
    /// </summary>
    public static void RegisterRecurringJobs(IRecurringJobManager recurring)
    {
        recurring.AddOrUpdate<ReservationLifecycleJob>(
            ReservationLifecycleJob.RecurringJobId,
            job => job.RunAsync(),
            ReservationLifecycleJob.DefaultCron);
    }
}
