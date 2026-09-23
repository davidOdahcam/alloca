using Alloca.Domain.Common;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Common.Settings;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Alloca.Domain.Services.Implementations;

public class ReservationService(
    IReservationRepository reservationRepository,
    IRoomRepository roomRepository,
    IDeskRepository deskRepository,
    IFloorRepository floorRepository,
    IPavilionRepository pavilionRepository,
    IBlockRepository blockRepository,
    IUserSuspensionRepository userSuspensionRepository,
    IUnitOfWork uow,
    IOptions<ReservationPolicySettings> policyOpts) : IReservationService
{
    private readonly ReservationPolicySettings _policy = policyOpts.Value;

    public async Task<Reservation> CreateAsync(Guid userId, ResourceType resourceType, Guid resourceId, TimeRange period, string? notes, DateTime nowUtc, CancellationToken ct = default)
    {
        var startUtc = period.StartUtc;
        var endUtc = period.EndUtc;

        if (await userSuspensionRepository.IsCurrentlySuspendedAsync(userId, nowUtc, ct))
            throw new ForbiddenException(ErrorCodes.UserSuspended, "Sua conta está suspensa no momento.");

        var duration = period.Duration;
        if (duration < TimeSpan.FromMinutes(_policy.MinDurationMinutes))
            throw new BusinessRuleException(ErrorCodes.ReservationMinDuration, $"A duração mínima é de {_policy.MinDurationMinutes} min.", _policy.MinDurationMinutes);
        if (duration > TimeSpan.FromMinutes(_policy.MaxDurationMinutes))
            throw new BusinessRuleException(ErrorCodes.ReservationMaxDuration, $"A duração máxima é de {_policy.MaxDurationMinutes} min.", _policy.MaxDurationMinutes);

        var pavilionId = await ResolvePavilionIdAsync(resourceType, resourceId, ct);
        var pavilion = await pavilionRepository.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var minStart = nowUtc.AddMinutes(pavilion.MinAdvanceMinutes);
        var maxStart = nowUtc.AddDays(pavilion.MaxAdvanceDays);
        if (period.StartUtc < minStart)
            throw new BusinessRuleException(ErrorCodes.ReservationMinAdvance, $"A reserva precisa começar pelo menos {pavilion.MinAdvanceMinutes} min após o momento atual.", pavilion.MinAdvanceMinutes);
        if (period.StartUtc > maxStart)
            throw new BusinessRuleException(ErrorCodes.ReservationMaxAdvance, $"A reserva não pode ultrapassar {pavilion.MaxAdvanceDays} dias de antecedência.", pavilion.MaxAdvanceDays);

        if (!pavilion.IsWithinOperatingHours(period))
            throw new BusinessRuleException(ErrorCodes.ReservationOutsideHours, "Horário fora do funcionamento do pavilhão.");

        if (!pavilion.IsAlignedToSlot(period))
            throw new BusinessRuleException(ErrorCodes.ReservationSlotAlignment, $"Os horários devem estar alinhados em blocos de {pavilion.SlotMinutes} minutos.", pavilion.SlotMinutes);

        var activeCount = await reservationRepository.CountActiveByUserAsync(userId, nowUtc, ct);
        if (activeCount >= _policy.MaxActiveReservations)
            throw new BusinessRuleException(ErrorCodes.ReservationActiveLimit, $"Você atingiu o limite de {_policy.MaxActiveReservations} reservas ativas.", _policy.MaxActiveReservations);

        if (await blockRepository.AnyBlockingAsync(BlockTargetType.Pavilion, pavilionId, startUtc, endUtc, ct))
            throw new ConflictException(ErrorCodes.ReservationPavilionBlocked, "O pavilhão está bloqueado nesse período.");

        if (resourceType == ResourceType.Room)
        {
            if (await blockRepository.AnyBlockingAsync(BlockTargetType.Room, resourceId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationRoomBlocked, "A sala está bloqueada nesse período.");
        }
        else
        {
            var desk = await deskRepository.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            if (await blockRepository.AnyBlockingAsync(BlockTargetType.Desk, desk.Id, startUtc, endUtc, ct)
                || await blockRepository.AnyBlockingAsync(BlockTargetType.Room, desk.RoomId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationResourceBlocked, "A mesa ou a sala está bloqueada nesse período.");
        }

        if (await reservationRepository.HasConflictAsync(resourceType, resourceId, startUtc, endUtc, ct))
            throw new ConflictException(ErrorCodes.ReservationConflict, "O recurso não está disponível nesse período.");

        if (resourceType == ResourceType.Room)
        {
            // Sala não pode ser reservada se qualquer mesa dela já estiver reservada no período
            if (await reservationRepository.HasAnyDeskConflictInRoomAsync(resourceId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationConflict, "Existe uma reserva de mesa nesta sala nesse período.");
        }
        else
        {
            // Mesa não pode ser reservada se a sala pai já estiver reservada no período
            var deskParent = await deskRepository.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            if (await reservationRepository.HasConflictAsync(ResourceType.Room, deskParent.RoomId, startUtc, endUtc, ct))
                throw new ConflictException(ErrorCodes.ReservationConflict, "A sala desta mesa já está reservada nesse período.");
        }

        var reservation = resourceType == ResourceType.Room
            ? Reservation.ForRoom(userId, pavilionId, resourceId, period, notes)
            : Reservation.ForDesk(userId, pavilionId, resourceId, period, notes);

        reservationRepository.Add(reservation);
        await uow.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task CancelAsync(Guid reservationId, Guid userId, DateTime nowUtc, CancellationToken ct = default)
    {
        var reservation = await LoadOwnedAsync(reservationId, userId, "Você só pode cancelar suas próprias reservas.", ct);
        try { reservation.CancelByUser(nowUtc, _policy.CancellationCutoffHours); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(Guid reservationId, Guid userId, string scannedExternalId, DateTime nowUtc, CancellationToken ct = default)
    {
        var reservation = await LoadOwnedAsync(reservationId, userId, "Você só pode fazer check-in nas suas próprias reservas.", ct);

        var scanned = (scannedExternalId ?? string.Empty).Trim().ToUpperInvariant();
        var expected = await GetResourceExternalIdAsync(reservation, ct);
        if (scanned != expected)
            throw new BusinessRuleException(ErrorCodes.ReservationCheckInWrongQr, "O QR Code lido não corresponde ao recurso reservado.");

        try { reservation.CheckIn(nowUtc, _policy.NoShowGraceMinutes); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task<string> GetQrPayloadAsync(Guid reservationId, Guid userId, CancellationToken ct = default)
    {
        var reservation = await LoadOwnedAsync(reservationId, userId, "Você só pode visualizar o QR Code das suas próprias reservas.", ct);
        return await GetResourceExternalIdAsync(reservation, ct);
    }

    public Task<IReadOnlyList<ReservationView>> ListByUserAsync(Guid userId, CancellationToken ct = default)
        => reservationRepository.ListByUserAsync(userId, ct);

    private async Task<Reservation> LoadOwnedAsync(Guid reservationId, Guid userId, string ownerMessage, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");
        if (reservation.UserId != userId)
            throw new ForbiddenException(ErrorCodes.ReservationOwnerOnly, ownerMessage);
        return reservation;
    }

    private async Task<Guid> ResolvePavilionIdAsync(ResourceType type, Guid resourceId, CancellationToken ct)
    {
        if (type == ResourceType.Room)
        {
            var room = await roomRepository.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            if (!room.IsReservable) throw new BusinessRuleException(ErrorCodes.RoomNotReservable, "Esta sala não aceita reservas.");
            var floor = await floorRepository.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
            return floor.PavilionId;
        }
        else
        {
            var desk = await deskRepository.GetByIdAsync(resourceId, ct)
                ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
            if (!desk.IsReservable) throw new BusinessRuleException(ErrorCodes.DeskNotReservable, "Esta mesa não aceita reservas.");
            var room = await roomRepository.GetByIdAsync(desk.RoomId, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            var floor = await floorRepository.GetByIdAsync(room.FloorId, ct)
                ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
            return floor.PavilionId;
        }
    }

    private async Task<string> GetResourceExternalIdAsync(Reservation reservation, CancellationToken ct)
    {
        if (reservation.ResourceType == ResourceType.Room)
        {
            var room = await roomRepository.GetByIdAsync(reservation.RoomId!.Value, ct)
                ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
            return room.ExternalId;
        }
        var desk = await deskRepository.GetByIdAsync(reservation.DeskId!.Value, ct)
            ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
        return desk.ExternalId;
    }
}
