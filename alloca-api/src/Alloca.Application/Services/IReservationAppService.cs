using Alloca.Application.DTOs.Reservations;

namespace Alloca.Application.Services;

public interface IReservationAppService
{
    Task<CreateReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid reservationId, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationResponse>> ListMineAsync(CancellationToken ct = default);
}
