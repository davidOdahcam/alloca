using Alloca.Domain.Common.Settings;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Alloca.Domain.Services.Implementations;

public class ReservationLifecycleService(
    IReservationRepository reservationRepository,
    IUserStrikeRepository userStrikeRepository,
    IUserSuspensionRepository userSuspensionRepository,
    IUnitOfWork uow,
    IOptions<ReservationPolicySettings> policyOpts) : IReservationLifecycleService
{
    private readonly ReservationPolicySettings _policy = policyOpts.Value;

    public async Task<(int NoShows, int Completed)> RunAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var cutoff = nowUtc.AddMinutes(-_policy.NoShowGraceMinutes);

        var approved = await reservationRepository.ListApprovedPastGraceAsync(cutoff, ct);
        foreach (var r in approved)
        {
            r.MarkNoShow(nowUtc, _policy.NoShowGraceMinutes);
            if (r.Status == ReservationStatus.NoShow)
            {
                userStrikeRepository.Add(new UserStrike(r.UserId, r.Id, nowUtc, _policy.StrikeWindowDays));

                var activeStrikes = await userStrikeRepository.CountActiveAsync(r.UserId, nowUtc, ct) + 1;
                if (activeStrikes >= _policy.StrikeSuspensionThreshold
                    && !await userSuspensionRepository.IsCurrentlySuspendedAsync(r.UserId, nowUtc, ct))
                {
                    userSuspensionRepository.Add(new UserSuspension(
                        r.UserId, nowUtc, nowUtc.AddDays(_policy.StrikeSuspensionDays),
                        $"Auto: {_policy.StrikeSuspensionThreshold} userStrikeRepository accumulated.", null));
                }
            }
        }

        var inProgress = await reservationRepository.ListInProgressPastEndAsync(nowUtc, ct);
        foreach (var r in inProgress) r.MarkCompleted(nowUtc);

        var noShows = approved.Count(r => r.Status == ReservationStatus.NoShow);
        if (approved.Count > 0 || inProgress.Count > 0)
            await uow.SaveChangesAsync(ct);

        return (noShows, inProgress.Count);
    }
}
