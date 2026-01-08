using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alloca.Application.Features.Reservations;

public record CreateReservationCommand(
    ResourceType ResourceType,
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes) : IRequest<Guid>;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
    }
}

public class CreateReservationCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IOptions<ReservationPolicySettings> policyOpts) : IRequestHandler<CreateReservationCommand, Guid>
{
    private readonly ReservationPolicySettings _policy = policyOpts.Value;

    public async Task<Guid> Handle(CreateReservationCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null) throw new UnauthorizedException("User not authenticated.");
        var userId = currentUser.UserId.Value;

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        // 1. Active suspension blocks creation
        var now = clock.UtcNow;
        var suspended = await db.UserSuspensions.AnyAsync(s =>
            s.UserId == userId && s.StartsAt <= now && now < s.EndsAt, ct);
        if (suspended) throw new ForbiddenException("User is currently suspended.");

        // 2. Duration
        var duration = period.Duration;
        if (duration < TimeSpan.FromMinutes(_policy.MinDurationMinutes))
            throw new BusinessRuleException($"Minimum duration is {_policy.MinDurationMinutes} min.");
        if (duration > TimeSpan.FromMinutes(_policy.MaxDurationMinutes))
            throw new BusinessRuleException($"Maximum duration is {_policy.MaxDurationMinutes} min.");

        // 3. Resolve pavilion + resource
        Guid pavilionId;
        Pavilion pavilion;
        if (request.ResourceType == ResourceType.Room)
        {
            var room = await db.Rooms
                .Include(r => r.Desks)
                .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
                ?? throw new NotFoundException("Room not found.");
            if (!room.IsReservable) throw new BusinessRuleException("Room is not reservable.");
            var floor = await db.Floors.FirstAsync(f => f.Id == room.FloorId, ct);
            pavilionId = floor.PavilionId;
        }
        else
        {
            var desk = await db.Desks.FirstOrDefaultAsync(d => d.Id == request.ResourceId, ct)
                ?? throw new NotFoundException("Desk not found.");
            var room = await db.Rooms.FirstAsync(r => r.Id == desk.RoomId, ct);
            var floor = await db.Floors.FirstAsync(f => f.Id == room.FloorId, ct);
            pavilionId = floor.PavilionId;
        }

        pavilion = await db.Pavilions
            .Include(p => p.OperatingHours)
            .FirstAsync(p => p.Id == pavilionId, ct);

        // 4. Advance window
        var minStart = now.AddMinutes(pavilion.MinAdvanceMinutes);
        var maxStart = now.AddDays(pavilion.MaxAdvanceDays);
        if (period.StartUtc < minStart)
            throw new BusinessRuleException($"Reservation must start at least {pavilion.MinAdvanceMinutes} min from now.");
        if (period.StartUtc > maxStart)
            throw new BusinessRuleException($"Reservation cannot exceed {pavilion.MaxAdvanceDays} days advance.");

        // 5. Operating hours
        if (!IsWithinOperatingHours(pavilion, period))
            throw new BusinessRuleException("Outside pavilion operating hours.");

        // 6. Slot alignment
        if (pavilion.SlotMinutes > 0)
        {
            if (period.StartUtc.Minute % pavilion.SlotMinutes != 0 || period.StartUtc.Second != 0
                || period.EndUtc.Minute % pavilion.SlotMinutes != 0 || period.EndUtc.Second != 0)
                throw new BusinessRuleException($"Times must align to {pavilion.SlotMinutes}-minute slots.");
        }

        // 7. User active reservations limit
        var activeStatuses = new[] { ReservationStatus.Pending, ReservationStatus.Approved, ReservationStatus.InProgress };
        var activeCount = await db.Reservations.CountAsync(r =>
            r.UserId == userId && activeStatuses.Contains(r.Status) && r.Period.EndUtc > now, ct);
        if (activeCount >= _policy.MaxActiveReservations)
            throw new BusinessRuleException($"Maximum of {_policy.MaxActiveReservations} active reservations reached.");

        // 8. Blocks
        var blockedPavilion = await db.Blocks.AnyAsync(b =>
            b.TargetType == BlockTargetType.Pavilion && b.TargetId == pavilionId &&
            b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc, ct);
        if (blockedPavilion) throw new ConflictException("Pavilion is blocked in this period.");

        if (request.ResourceType == ResourceType.Room)
        {
            var blockedRoom = await db.Blocks.AnyAsync(b =>
                b.TargetType == BlockTargetType.Room && b.TargetId == request.ResourceId &&
                b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc, ct);
            if (blockedRoom) throw new ConflictException("Room is blocked in this period.");
        }
        else
        {
            var desk = await db.Desks.FirstAsync(d => d.Id == request.ResourceId, ct);
            var blockedDeskOrRoom = await db.Blocks.AnyAsync(b =>
                ((b.TargetType == BlockTargetType.Desk && b.TargetId == desk.Id) ||
                 (b.TargetType == BlockTargetType.Room && b.TargetId == desk.RoomId)) &&
                b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc, ct);
            if (blockedDeskOrRoom) throw new ConflictException("Desk/room is blocked in this period.");
        }

        // 9. Resource conflict with existing active reservations
        bool conflict;
        if (request.ResourceType == ResourceType.Room)
        {
            conflict = await db.Reservations.AnyAsync(r =>
                r.ResourceType == ResourceType.Room && r.RoomId == request.ResourceId &&
                activeStatuses.Contains(r.Status) &&
                r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc, ct);
        }
        else
        {
            conflict = await db.Reservations.AnyAsync(r =>
                r.ResourceType == ResourceType.Desk && r.DeskId == request.ResourceId &&
                activeStatuses.Contains(r.Status) &&
                r.Period.StartUtc < endUtc && startUtc < r.Period.EndUtc, ct);
        }
        if (conflict) throw new ConflictException("Resource is not available in this period.");

        // 10. Create
        var reservation = request.ResourceType == ResourceType.Room
            ? Reservation.ForRoom(userId, pavilionId, request.ResourceId, period, request.Notes)
            : Reservation.ForDesk(userId, pavilionId, request.ResourceId, period, request.Notes);

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);
        return reservation.Id;
    }

    private static bool IsWithinOperatingHours(Pavilion pavilion, TimeRange period)
    {
        var startLocal = period.StartUtc.ToLocalTime();
        var endLocal = period.EndUtc.ToLocalTime();
        if (startLocal.Date != endLocal.Date) return false;
        var oh = pavilion.OperatingHours.FirstOrDefault(o => o.DayOfWeek == startLocal.DayOfWeek);
        if (oh is null || oh.IsClosed) return false;
        var start = TimeOnly.FromDateTime(startLocal);
        var end = TimeOnly.FromDateTime(endLocal);
        return start >= oh.OpensAt && end <= oh.ClosesAt;
    }
}
