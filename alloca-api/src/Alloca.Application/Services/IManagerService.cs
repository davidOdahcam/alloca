using Alloca.Application.DTOs.Manager;
using Alloca.Application.DTOs.Reservations;

namespace Alloca.Application.Services;

public interface IManagerService
{
    Task<IReadOnlyList<ReservationResponse>> ListPendingAsync(Guid? pavilionId, CancellationToken ct = default);
    Task ApproveAsync(Guid reservationId, CancellationToken ct = default);
    Task RejectAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default);
    Task RevokeAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default);
}
