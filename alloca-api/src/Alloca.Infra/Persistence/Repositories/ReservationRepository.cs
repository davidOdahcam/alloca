using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class ReservationRepository(AllocaDbContext db) : Repository<Reservation>(db), IReservationRepository
{
    private static readonly ReservationStatus[] ActiveStatuses =
        [ReservationStatus.Pending, ReservationStatus.Approved, ReservationStatus.InProgress];

    public Task<int> CountActiveByUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
        => Set.CountAsync(r =>
            r.UserId == userId && ActiveStatuses.Contains(r.Status) && r.Period.EndUtc > nowUtc, ct);

    public Task<bool> HasConflictAsync(ResourceType type, Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        if (type == ResourceType.Room)
        {
            return Set.AnyAsync(r =>
                r.ResourceType == ResourceType.Room && r.RoomId == resourceId
                && ActiveStatuses.Contains(r.Status)
                && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc, ct);
        }
        return Set.AnyAsync(r =>
            r.ResourceType == ResourceType.Desk && r.DeskId == resourceId
            && ActiveStatuses.Contains(r.Status)
            && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc, ct);
    }

    public Task<bool> HasAnyDeskConflictInRoomAsync(Guid roomId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var desksInRoom = Db.Set<Desk>().Where(d => d.RoomId == roomId).Select(d => d.Id);
        return Set.AnyAsync(r =>
            r.ResourceType == ResourceType.Desk && r.DeskId != null
            && desksInRoom.Contains(r.DeskId!.Value)
            && ActiveStatuses.Contains(r.Status)
            && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc, ct);
    }

    public async Task<IReadOnlyList<Reservation>> ListApprovedPastGraceAsync(DateTime cutoffUtc, CancellationToken ct = default)
        => await Set.Where(r => r.Status == ReservationStatus.Approved && r.Period.StartUtc < cutoffUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Reservation>> ListInProgressPastEndAsync(DateTime nowUtc, CancellationToken ct = default)
        => await Set.Where(r => r.Status == ReservationStatus.InProgress && r.Period.EndUtc <= nowUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListConflictingRoomIdsAsync(IEnumerable<Guid> roomIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var ids = roomIds.ToHashSet();
        return await Set.Where(r =>
            r.ResourceType == ResourceType.Room && r.RoomId != null && ids.Contains(r.RoomId!.Value)
            && ActiveStatuses.Contains(r.Status)
            && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc)
            .Select(r => r.RoomId!.Value).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> ListConflictingDeskIdsAsync(IEnumerable<Guid> deskIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var ids = deskIds.ToHashSet();
        return await Set.Where(r =>
            r.ResourceType == ResourceType.Desk && r.DeskId != null && ids.Contains(r.DeskId!.Value)
            && ActiveStatuses.Contains(r.Status)
            && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc)
            .Select(r => r.DeskId!.Value).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationView>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var query =
            from r in Set
            where r.UserId == userId
            join pav in Db.Pavilions on r.PavilionId equals pav.Id
            join room in Db.Rooms on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in Db.Desks on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.Period.StartUtc descending
            select new ReservationView(
                r.Id, r.ResourceType, r.RoomId, r.DeskId,
                r.ResourceType == ResourceType.Room ? room!.ExternalId : desk!.ExternalId,
                r.ResourceType == ResourceType.Room ? room!.Name : desk!.Name,
                r.PavilionId, pav.Name, r.Period.StartUtc, r.Period.EndUtc,
                r.Status, r.Notes, r.DecisionReason, r.CheckedInAt, r.CreatedAt);

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationView>> ListPendingByPavilionsAsync(IReadOnlyCollection<Guid> pavilionIds, Guid? pavilionFilter, CancellationToken ct = default)
    {
        var query =
            from r in Set
            where r.Status == ReservationStatus.Pending
                && pavilionIds.Contains(r.PavilionId)
                && (pavilionFilter == null || r.PavilionId == pavilionFilter)
            join pav in Db.Pavilions on r.PavilionId equals pav.Id
            join room in Db.Rooms on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in Db.Desks on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.CreatedAt
            select new ReservationView(
                r.Id, r.ResourceType, r.RoomId, r.DeskId,
                r.ResourceType == ResourceType.Room ? room!.ExternalId : desk!.ExternalId,
                r.ResourceType == ResourceType.Room ? room!.Name : desk!.Name,
                r.PavilionId, pav.Name, r.Period.StartUtc, r.Period.EndUtc,
                r.Status, r.Notes, r.DecisionReason, r.CheckedInAt, r.CreatedAt);

        return await query.ToListAsync(ct);
    }
}
