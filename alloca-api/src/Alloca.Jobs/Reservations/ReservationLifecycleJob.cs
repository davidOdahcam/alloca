using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Alloca.Jobs.Reservations;

/// <summary>
/// Job recorrente responsável por transitar estados de reservas:
/// <list type="bullet">
///   <item><c>Approved</c> → <c>NoShow</c> quando o usuário não fez check-in dentro do grace period.</item>
///   <item><c>InProgress</c> → <c>Completed</c> quando o período da reserva já encerrou.</item>
/// </list>
/// Também aplica strikes e suspende usuários que excedem o limite configurado.
/// A regra de negócio reside no serviço de domínio; este job apenas a aciona.
/// </summary>
public class ReservationLifecycleJob(
    IReservationLifecycleService lifecycleService,
    IDateTimeProvider clock,
    ILogger<ReservationLifecycleJob> logger)
{
    /// <summary>Identificador estável do job no Hangfire.</summary>
    public const string RecurringJobId = "reservation-lifecycle";

    /// <summary>Expressão cron padrão (a cada minuto).</summary>
    public const string DefaultCron = "* * * * *";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync()
    {
        var (noShows, completed) = await lifecycleService.RunAsync(clock.UtcNow);

        if (noShows > 0 || completed > 0)
            logger.LogInformation(
                "Reservation lifecycle: {NoShow} no-shows, {Completed} completed.",
                noShows, completed);
    }
}
