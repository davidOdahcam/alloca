using Alloca.Domain.Entities;
using Alloca.Domain.Enums;

namespace Alloca.Domain.Repositories;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<int> CountActiveByUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
    Task<bool> HasConflictAsync(ResourceType type, Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> ListApprovedPastGraceAsync(DateTime cutoffUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> ListInProgressPastEndAsync(DateTime nowUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListConflictingRoomIdsAsync(IEnumerable<Guid> roomIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListConflictingDeskIdsAsync(IEnumerable<Guid> deskIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}
