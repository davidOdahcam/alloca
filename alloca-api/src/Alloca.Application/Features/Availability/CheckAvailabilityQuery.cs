using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Availability;

public record AvailabilityResource(
    Guid Id,
    string ExternalId,
    string Name,
    ResourceType Type,
    Guid? RoomId,
    bool Available);

public record CheckAvailabilityQuery(
    Guid PavilionId,
    Guid FloorId,
    DateTime StartUtc,
    DateTime EndUtc) : IRequest<IReadOnlyList<AvailabilityResource>>;

public class CheckAvailabilityQueryValidator : AbstractValidator<CheckAvailabilityQuery>
{
    public CheckAvailabilityQueryValidator()
    {
        RuleFor(x => x.PavilionId).NotEmpty();
        RuleFor(x => x.FloorId).NotEmpty();
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
    }
}

public class CheckAvailabilityQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<CheckAvailabilityQuery, IReadOnlyList<AvailabilityResource>>
{
    public async Task<IReadOnlyList<AvailabilityResource>> Handle(CheckAvailabilityQuery request, CancellationToken ct)
    {
        var pavilion = await db.Pavilions
            .Include(p => p.OperatingHours)
            .FirstOrDefaultAsync(p => p.Id == request.PavilionId, ct)
            ?? throw new NotFoundException("Pavilion not found.");

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        if (!IsWithinOperatingHours(pavilion, period))
            return Array.Empty<AvailabilityResource>();

        var floor = await db.Floors
            .Include(f => f.Rooms).ThenInclude(r => r.Desks)
            .FirstOrDefaultAsync(f => f.Id == request.FloorId && f.PavilionId == request.PavilionId, ct)
            ?? throw new NotFoundException("Floor not found.");

        var rooms = floor.Rooms.ToList();
        var roomIds = rooms.Select(r => r.Id).ToHashSet();
        var deskIds = rooms.SelectMany(r => r.Desks).Select(d => d.Id).ToHashSet();

        // Pavilion-level blocks
        var pavilionBlocked = await db.Blocks.AnyAsync(b =>
            b.TargetType == BlockTargetType.Pavilion &&
            b.TargetId == request.PavilionId &&
            b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc, ct);

        if (pavilionBlocked) return Array.Empty<AvailabilityResource>();

        var blockedRoomIds = await db.Blocks
            .Where(b => b.TargetType == BlockTargetType.Room && roomIds.Contains(b.TargetId)
                && b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc)
            .Select(b => b.TargetId).ToListAsync(ct);

        var blockedDeskIds = await db.Blocks
            .Where(b => b.TargetType == BlockTargetType.Desk && deskIds.Contains(b.TargetId)
                && b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc)
            .Select(b => b.TargetId).ToListAsync(ct);

        // Active reservations (Pending + Approved + InProgress)
        var activeStatuses = new[]
        {
            ReservationStatus.Pending, ReservationStatus.Approved, ReservationStatus.InProgress
        };

        var conflictingRoomIds = await db.Reservations
            .Where(r => r.ResourceType == ResourceType.Room && r.RoomId != null
                && roomIds.Contains(r.RoomId!.Value)
                && activeStatuses.Contains(r.Status)
                && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc)
            .Select(r => r.RoomId!.Value).ToListAsync(ct);

        var conflictingDeskIds = await db.Reservations
            .Where(r => r.ResourceType == ResourceType.Desk && r.DeskId != null
                && deskIds.Contains(r.DeskId!.Value)
                && activeStatuses.Contains(r.Status)
                && r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc)
            .Select(r => r.DeskId!.Value).ToListAsync(ct);

        var blockedRooms = blockedRoomIds.ToHashSet();
        var blockedDesks = blockedDeskIds.ToHashSet();
        var busyRooms = conflictingRoomIds.ToHashSet();
        var busyDesks = conflictingDeskIds.ToHashSet();

        var result = new List<AvailabilityResource>();
        foreach (var room in rooms)
        {
            if (room.IsReservable)
            {
                var available = !blockedRooms.Contains(room.Id) && !busyRooms.Contains(room.Id);
                result.Add(new AvailabilityResource(room.Id, room.ExternalId, room.Name, ResourceType.Room, null, available));
            }
            foreach (var desk in room.Desks)
            {
                var available = !blockedRooms.Contains(room.Id)
                    && !blockedDesks.Contains(desk.Id)
                    && !busyDesks.Contains(desk.Id);
                result.Add(new AvailabilityResource(desk.Id, desk.ExternalId, desk.Name, ResourceType.Desk, room.Id, available));
            }
        }
        return result;
    }

    private static bool IsWithinOperatingHours(Pavilion pavilion, TimeRange period)
    {
        var startLocal = period.StartUtc.ToLocalTime();
        var endLocal = period.EndUtc.ToLocalTime();
        if (startLocal.Date != endLocal.Date) return false; // simple rule: same day
        var oh = pavilion.OperatingHours.FirstOrDefault(o => o.DayOfWeek == startLocal.DayOfWeek);
        if (oh is null || oh.IsClosed) return false;
        var start = TimeOnly.FromDateTime(startLocal);
        var end = TimeOnly.FromDateTime(endLocal);
        return start >= oh.OpensAt && end <= oh.ClosesAt;
    }
}
