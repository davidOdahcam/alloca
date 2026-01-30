using Alloca.Application.DTOs.Reservations;

namespace Alloca.Application.Services;

public interface IReservationService
{
    Task<CreateReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid reservationId, CancellationToken ct = default);
    Task CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> ListMineAsync(CancellationToken ct = default);
    Task<(byte[] Png, string Payload)> GetQrCodeAsync(Guid reservationId, CancellationToken ct = default);
}
