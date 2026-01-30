using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Application.DTOs.Reservations;
using Alloca.Domain.Common;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Alloca.Application.Services.Implementations;

public class ReservationService(
    IReservationRepository reservations,
    IRoomRepository rooms,
    IDeskRepository desks,
    IFloorRepository floors,
    IPavilionRepository pavilions,
    IBlockRepository blocks,
    IUserSuspensionRepository suspensions,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IQrCodeService qrCode,
    IOptions<ReservationPolicySettings> policyOpts,
    IValidator<CreateReservationRequest> createValidator) : IReservationService
{
    private readonly ReservationPolicySettings _policy = policyOpts.Value;

    public async Task<CreateReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var userId = RequireUserId();

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);
        var now = clock.UtcNow;

        if (await suspensions.IsCurrentlySuspendedAsync(userId, now, ct))
            throw new ForbiddenException("User is currently suspended.");

        var duration = period.Duration;
        if (duration < TimeSpan.FromMinutes(_policy.MinDurationMinutes))
            throw new BusinessRuleException($"Minimum duration is {_policy.MinDurationMinutes} min.");
        if (duration > TimeSpan.FromMinutes(_policy.MaxDurationMinutes))
            throw new BusinessRuleException($"Maximum duration is {_policy.MaxDurationMinutes} min.");

        var pavilionId = await ResolvePavilionIdAsync(request.ResourceType, request.ResourceId, ct);
        var pavilion = await pavilions.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException("Pavilion not found.");

        var minStart = now.AddMinutes(pavilion.MinAdvanceMinutes);
        var maxStart = now.AddDays(pavilion.MaxAdvanceDays);
        if (period.StartUtc < minStart)
            throw new BusinessRuleException($"Reservation must start at least {pavilion.MinAdvanceMinutes} min from now.");
        if (period.StartUtc > maxStart)
            throw new BusinessRuleException($"Reservation cannot exceed {pavilion.MaxAdvanceDays} days advance.");

        if (!IsWithinOperatingHours(pavilion, period))
            throw new BusinessRuleException("Outside pavilion operating hours.");

        if (pavilion.SlotMinutes > 0 && !IsAlignedToSlot(period, pavilion.SlotMinutes))
            throw new BusinessRuleException($"Times must align to {pavilion.SlotMinutes}-minute slots.");

        var activeCount = await reservations.CountActiveByUserAsync(userId, now, ct);
        if (activeCount >= _policy.MaxActiveReservations)
            throw new BusinessRuleException($"Maximum of {_policy.MaxActiveReservations} active reservations reached.");

        if (await blocks.AnyBlockingAsync(BlockTargetType.Pavilion, pavilionId, startUtc, endUtc, ct))
            throw new ConflictException("Pavilion is blocked in this period.");

        if (request.ResourceType == ResourceType.Room)
        {
            if (await blocks.AnyBlockingAsync(BlockTargetType.Room, request.ResourceId, startUtc, endUtc, ct))
                throw new ConflictException("Room is blocked in this period.");
        }
        else
        {
            var desk = await desks.GetByIdAsync(request.ResourceId, ct)
                ?? throw new NotFoundException("Desk not found.");
            if (await blocks.AnyBlockingAsync(BlockTargetType.Desk, desk.Id, startUtc, endUtc, ct)
                || await blocks.AnyBlockingAsync(BlockTargetType.Room, desk.RoomId, startUtc, endUtc, ct))
                throw new ConflictException("Desk/room is blocked in this period.");
        }

        if (await reservations.HasConflictAsync(request.ResourceType, request.ResourceId, startUtc, endUtc, ct))
            throw new ConflictException("Resource is not available in this period.");

        var reservation = request.ResourceType == ResourceType.Room
            ? Reservation.ForRoom(userId, pavilionId, request.ResourceId, period, request.Notes)
            : Reservation.ForDesk(userId, pavilionId, request.ResourceId, period, request.Notes);

        reservations.Add(reservation);
        await uow.SaveChangesAsync(ct);
        return new CreateReservationResponse(reservation.Id);
    }

    public async Task CancelAsync(Guid reservationId, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var reservation = await reservations.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        if (reservation.UserId != userId)
            throw new ForbiddenException("You can only cancel your own reservations.");

        try { reservation.CancelByUser(clock.UtcNow, _policy.CancellationCutoffHours); }
        catch (DomainException ex) { throw new BusinessRuleException(ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var reservation = await reservations.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        if (reservation.UserId != userId)
            throw new ForbiddenException("You can only check-in your own reservations.");

        var scanned = (request.ScannedExternalId ?? string.Empty).Trim().ToUpperInvariant();
        var expected = await GetResourceExternalIdAsync(reservation, ct);
        if (scanned != expected)
            throw new BusinessRuleException("Scanned QR does not match the reserved resource.");

        try { reservation.CheckIn(clock.UtcNow, _policy.NoShowGraceMinutes); }
        catch (DomainException ex) { throw new BusinessRuleException(ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationResponse>> ListMineAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var query =
            from r in reservations.Query()
            where r.UserId == userId
            join pav in pavilions.Query() on r.PavilionId equals pav.Id
            join room in rooms.Query() on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in desks.Query() on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.Period.StartUtc descending
            select new ReservationResponse(
                r.Id, r.ResourceType, r.RoomId, r.DeskId,
                r.ResourceType == ResourceType.Room ? room!.ExternalId : desk!.ExternalId,
                r.ResourceType == ResourceType.Room ? room!.Name : desk!.Name,
                r.PavilionId, pav.Name, r.Period.StartUtc, r.Period.EndUtc,
                r.Status, r.Notes, r.DecisionReason, r.CheckedInAt, r.CreatedAt);

        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query, ct);
    }

    public async Task<(byte[] Png, string Payload)> GetQrCodeAsync(Guid reservationId, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var reservation = await reservations.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        if (reservation.UserId != userId)
            throw new ForbiddenException("You can only view your own reservation's QR code.");

        var payload = await GetResourceExternalIdAsync(reservation, ct);
        var png = qrCode.GeneratePng(payload);
        return (png, payload);
    }

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException("User not authenticated.");
        return currentUser.UserId.Value;
    }

    private async Task<Guid> ResolvePavilionIdAsync(ResourceType type, Guid resourceId, CancellationToken ct)
    {
        if (type == ResourceType.Room)
        {
            var room = await rooms.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException("Room not found.");
            if (!room.IsReservable) throw new BusinessRuleException("Room is not reservable.");
            var floor = await floors.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException("Floor not found.");
            return floor.PavilionId;
        }
        else
        {
            var desk = await desks.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException("Desk not found.");
            var room = await rooms.GetByIdAsync(desk.RoomId, ct)
                ?? throw new NotFoundException("Room not found.");
            var floor = await floors.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException("Floor not found.");
            return floor.PavilionId;
        }
    }

    private async Task<string> GetResourceExternalIdAsync(Reservation reservation, CancellationToken ct)
    {
        if (reservation.ResourceType == ResourceType.Room)
        {
            var room = await rooms.GetByIdAsync(reservation.RoomId!.Value, ct)
                ?? throw new NotFoundException("Room not found.");
            return room.ExternalId;
        }
        var desk = await desks.GetByIdAsync(reservation.DeskId!.Value, ct)
            ?? throw new NotFoundException("Desk not found.");
        return desk.ExternalId;
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

    private static bool IsAlignedToSlot(TimeRange period, int slotMinutes) =>
        period.StartUtc.Minute % slotMinutes == 0 && period.StartUtc.Second == 0
        && period.EndUtc.Minute % slotMinutes == 0 && period.EndUtc.Second == 0;
}
