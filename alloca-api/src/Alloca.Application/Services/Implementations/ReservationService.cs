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
            throw new ForbiddenException(ErrorCodes.UserSuspended, "Sua conta está suspensa no momento.");

        var duration = period.Duration;
        if (duration < TimeSpan.FromMinutes(_policy.MinDurationMinutes))
            throw new BusinessRuleException(ErrorCodes.ReservationMinDuration, $"A duração mínima é de {_policy.MinDurationMinutes} min.", _policy.MinDurationMinutes);
        if (duration > TimeSpan.FromMinutes(_policy.MaxDurationMinutes))
            throw new BusinessRuleException(ErrorCodes.ReservationMaxDuration, $"A duração máxima é de {_policy.MaxDurationMinutes} min.", _policy.MaxDurationMinutes);

        var pavilionId = await ResolvePavilionIdAsync(request.ResourceType, request.ResourceId, ct);
        var pavilion = await pavilions.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var minStart = now.AddMinutes(pavilion.MinAdvanceMinutes);
        var maxStart = now.AddDays(pavilion.MaxAdvanceDays);
        if (period.StartUtc < minStart)
            throw new BusinessRuleException(ErrorCodes.ReservationMinAdvance, $"A reserva precisa começar pelo menos {pavilion.MinAdvanceMinutes} min após o momento atual.", pavilion.MinAdvanceMinutes);
        if (period.StartUtc > maxStart)
            throw new BusinessRuleException(ErrorCodes.ReservationMaxAdvance, $"A reserva não pode ultrapassar {pavilion.MaxAdvanceDays} dias de antecedência.", pavilion.MaxAdvanceDays);

        if (!IsWithinOperatingHours(pavilion, period))
            throw new BusinessRuleException(ErrorCodes.ReservationOutsideHours, "Horário fora do funcionamento do pavilhão.");

        if (pavilion.SlotMinutes > 0 && !IsAlignedToSlot(period, pavilion.SlotMinutes))
            throw new BusinessRuleException(ErrorCodes.ReservationSlotAlignment, $"Os horários devem estar alinhados em blocos de {pavilion.SlotMinutes} minutos.", pavilion.SlotMinutes);

        var activeCount = await reservations.CountActiveByUserAsync(userId, now, ct);
        if (activeCount >= _policy.MaxActiveReservations)
            throw new BusinessRuleException(ErrorCodes.ReservationActiveLimit, $"Você atingiu o limite de {_policy.MaxActiveReservations} reservas ativas.", _policy.MaxActiveReservations);

        if (await blocks.AnyBlockingAsync(BlockTargetType.Pavilion, pavilionId, startUtc, endUtc, ct))
            throw new ConflictException(ErrorCodes.ReservationPavilionBlocked, "O pavilhão está bloqueado nesse período.");

        if (request.ResourceType == ResourceType.Room)
        {
            if (await blocks.AnyBlockingAsync(BlockTargetType.Room, request.ResourceId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationRoomBlocked, "A sala está bloqueada nesse período.");
        }
        else
        {
            var desk = await desks.GetByIdAsync(request.ResourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            if (await blocks.AnyBlockingAsync(BlockTargetType.Desk, desk.Id, startUtc, endUtc, ct)
                || await blocks.AnyBlockingAsync(BlockTargetType.Room, desk.RoomId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationResourceBlocked, "A mesa ou a sala está bloqueada nesse período.");
        }

        if (await reservations.HasConflictAsync(request.ResourceType, request.ResourceId, startUtc, endUtc, ct))
            throw new ConflictException(ErrorCodes.ReservationConflict, "O recurso não está disponível nesse período.");

        if (request.ResourceType == ResourceType.Room)
        {
            // Sala não pode ser reservada se qualquer mesa dela já estiver reservada no período
            if (await reservations.HasAnyDeskConflictInRoomAsync(request.ResourceId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationConflict, "Existe uma reserva de mesa nesta sala nesse período.");
        }
        else
        {
            // Mesa não pode ser reservada se a sala pai já estiver reservada no período
            var deskParent = await desks.GetByIdAsync(request.ResourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            if (await reservations.HasConflictAsync(ResourceType.Room, deskParent.RoomId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationConflict, "A sala desta mesa já está reservada nesse período.");
        }

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
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");
        if (reservation.UserId != userId)
            throw new ForbiddenException(ErrorCodes.ReservationOwnerOnly, "Você só pode cancelar suas próprias reservas.");

        try { reservation.CancelByUser(clock.UtcNow, _policy.CancellationCutoffHours); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var reservation = await reservations.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");
        if (reservation.UserId != userId)
            throw new ForbiddenException(ErrorCodes.ReservationOwnerOnly, "Você só pode fazer check-in nas suas próprias reservas.");

        var scanned = (request.ScannedExternalId ?? string.Empty).Trim().ToUpperInvariant();
        var expected = await GetResourceExternalIdAsync(reservation, ct);
        if (scanned != expected)
            throw new BusinessRuleException(ErrorCodes.ReservationCheckInWrongQr, "O QR Code lido não corresponde ao recurso reservado.");

        try { reservation.CheckIn(clock.UtcNow, _policy.NoShowGraceMinutes); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
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
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");
        if (reservation.UserId != userId)
            throw new ForbiddenException(ErrorCodes.ReservationOwnerOnly, "Você só pode visualizar o QR Code das suas próprias reservas.");

        var payload = await GetResourceExternalIdAsync(reservation, ct);
        var png = qrCode.GeneratePng(payload);
        return (png, payload);
    }

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");
        return currentUser.UserId.Value;
    }

    private async Task<Guid> ResolvePavilionIdAsync(ResourceType type, Guid resourceId, CancellationToken ct)
    {
        if (type == ResourceType.Room)
        {
            var room = await rooms.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            if (!room.IsReservable) throw new BusinessRuleException(ErrorCodes.RoomNotReservable, "Esta sala não aceita reservas.");
            var floor = await floors.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
            return floor.PavilionId;
        }
        else
        {
            var desk = await desks.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            var room = await rooms.GetByIdAsync(desk.RoomId, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            var floor = await floors.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
            return floor.PavilionId;
        }
    }

    private async Task<string> GetResourceExternalIdAsync(Reservation reservation, CancellationToken ct)
    {
        if (reservation.ResourceType == ResourceType.Room)
        {
            var room = await rooms.GetByIdAsync(reservation.RoomId!.Value, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            return room.ExternalId;
        }
        var desk = await desks.GetByIdAsync(reservation.DeskId!.Value, ct)
            ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
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
