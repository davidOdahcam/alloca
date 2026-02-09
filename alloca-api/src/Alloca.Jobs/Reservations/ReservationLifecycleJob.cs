using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alloca.Jobs.Reservations;

/// <summary>
/// Job recorrente responsável por transitar estados de reservas:
/// <list type="bullet">
///   <item><c>Approved</c> → <c>NoShow</c> quando o usuário não fez check-in dentro do grace period.</item>
///   <item><c>InProgress</c> → <c>Completed</c> quando o período da reserva já encerrou.</item>
/// </list>
/// Também aplica strikes e suspende usuários que excedem o limite configurado.
/// </summary>
public class ReservationLifecycleJob(
    IReservationRepository reservations,
    IUserStrikeRepository strikes,
    IUserSuspensionRepository suspensions,
    IUnitOfWork uow,
    IDateTimeProvider clock,
    IOptions<ReservationPolicySettings> policyOpts,
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
        var policy = policyOpts.Value;
        var now = clock.UtcNow;
        var cutoff = now.AddMinutes(-policy.NoShowGraceMinutes);

        var approved = await reservations.ListApprovedPastGraceAsync(cutoff);

        foreach (var r in approved)
        {
            r.MarkNoShow(now, policy.NoShowGraceMinutes);
            if (r.Status == ReservationStatus.NoShow)
            {
                strikes.Add(new UserStrike(r.UserId, r.Id, now, policy.StrikeWindowDays));

                var activeStrikes = await strikes.CountActiveAsync(r.UserId, now) + 1;
                if (activeStrikes >= policy.StrikeSuspensionThreshold
                    && !await suspensions.IsCurrentlySuspendedAsync(r.UserId, now))
                {
                    suspensions.Add(new UserSuspension(
                        r.UserId, now, now.AddDays(policy.StrikeSuspensionDays),
                        $"Auto: {policy.StrikeSuspensionThreshold} strikes accumulated.", null));
                }
            }
        }

        var inProgress = await reservations.ListInProgressPastEndAsync(now);
        foreach (var r in inProgress) r.MarkCompleted(now);

        if (approved.Count > 0 || inProgress.Count > 0)
        {
            await uow.SaveChangesAsync();
            logger.LogInformation(
                "Reservation lifecycle: {NoShow} no-shows, {Completed} completed.",
                approved.Count(r => r.Status == ReservationStatus.NoShow),
                inProgress.Count);
        }
    }
}
