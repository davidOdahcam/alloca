using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Reservations;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Services;
using Alloca.Domain.ValueObjects;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class ReservationAppService(
    IReservationService reservationService,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IQrCodeService qrCode,
    IValidator<CreateReservationRequest> createValidator) : IReservationAppService
{
    public async Task<CreateReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var userId = RequireUserId();

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var reservation = await reservationService.CreateAsync(
            userId, request.ResourceType, request.ResourceId, period, request.Notes, clock.UtcNow, ct);

        return new CreateReservationResponse(reservation.Id);
    }

    public Task CancelAsync(Guid reservationId, CancellationToken ct = default)
        => reservationService.CancelAsync(reservationId, RequireUserId(), clock.UtcNow, ct);

    public Task CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default)
        => reservationService.CheckInAsync(reservationId, RequireUserId(), request.ScannedExternalId, clock.UtcNow, ct);

    public async Task<IReadOnlyList<ReservationResponse>> ListMineAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var views = await reservationService.ListByUserAsync(userId, ct);
        return [.. views.Select(ToResponse)];
    }

    public async Task<(byte[] Png, string Payload)> GetQrCodeAsync(Guid reservationId, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var payload = await reservationService.GetQrPayloadAsync(reservationId, userId, ct);
        var png = qrCode.GeneratePng(payload);
        return (png, payload);
    }

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");
        return currentUser.UserId.Value;
    }

    private static ReservationResponse ToResponse(ReservationView v) =>
        new(v.Id, v.ResourceType, v.RoomId, v.DeskId, v.ResourceExternalId, v.ResourceName,
            v.PavilionId, v.PavilionName, v.StartUtc, v.EndUtc, v.Status, v.Notes,
            v.DecisionReason, v.CheckedInAt, v.CreatedAt);
}
