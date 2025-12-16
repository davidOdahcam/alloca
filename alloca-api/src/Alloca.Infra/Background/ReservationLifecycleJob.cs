using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alloca.Infra.Background;

/// <summary>
/// Periodically transitions reservation states:
/// - Approved past start+grace without check-in => NoShow + strike (+ suspend if threshold reached)
/// - InProgress past end => Completed
/// - Expire suspensions implicitly via IsActive() checks (no DB op needed; left for cleanup)
/// </summary>
public class ReservationLifecycleJob(
    IAppDbContext db,
    IDateTimeProvider clock,
    IOptions<ReservationPolicySettings> policyOpts,
    ILogger<ReservationLifecycleJob> logger)
{
    public async Task RunAsync()
    {
        var policy = policyOpts.Value;
        var now = clock.UtcNow;

        var approved = await ((DbContext)db).Set<Reservation>()
            .Where(r => r.Status == ReservationStatus.Approved && r.Period.StartUtc.AddMinutes(policy.NoShowGraceMinutes) < now)
            .ToListAsync();

        foreach (var r in approved)
        {
            r.MarkNoShow(now, policy.NoShowGraceMinutes);
            if (r.Status == ReservationStatus.NoShow)
            {
                var strike = new UserStrike(r.UserId, r.Id, now, policy.StrikeWindowDays);
                ((DbContext)db).Set<UserStrike>().Add(strike);

                var activeStrikes = await ((DbContext)db).Set<UserStrike>()
                    .CountAsync(s => s.UserId == r.UserId && s.ExpiresAt > now) + 1;
                if (activeStrikes >= policy.StrikeSuspensionThreshold)
                {
                    var alreadySuspended = await ((DbContext)db).Set<UserSuspension>()
                        .AnyAsync(s => s.UserId == r.UserId && s.StartsAt <= now && now < s.EndsAt);
                    if (!alreadySuspended)
                    {
                        var sus = new UserSuspension(
                            r.UserId, now, now.AddDays(policy.StrikeSuspensionDays),
                            $"Auto: {policy.StrikeSuspensionThreshold} strikes accumulated.", null);
                        ((DbContext)db).Set<UserSuspension>().Add(sus);
                    }
                }
            }
        }

        var inProgress = await ((DbContext)db).Set<Reservation>()
            .Where(r => r.Status == ReservationStatus.InProgress && r.Period.EndUtc <= now)
            .ToListAsync();
        foreach (var r in inProgress) r.MarkCompleted(now);

        if (approved.Count > 0 || inProgress.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Reservation lifecycle: {NoShow} no-shows, {Completed} completed.",
                approved.Count(r => r.Status == ReservationStatus.NoShow), inProgress.Count);
        }
    }
}
