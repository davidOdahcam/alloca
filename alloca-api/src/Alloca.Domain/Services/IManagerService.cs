using Alloca.Domain.ReadModels;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio com as regras de moderação de reservas pelos gestores de pavilhão.
/// </summary>
public interface IManagerService
{
    Task<IReadOnlyList<ReservationView>> ListPendingAsync(Guid actorUserId, bool isAdmin, Guid? pavilionId, CancellationToken ct = default);
    Task ApproveAsync(Guid reservationId, Guid actorUserId, bool isAdmin, CancellationToken ct = default);
    Task RejectAsync(Guid reservationId, Guid actorUserId, bool isAdmin, string reason, CancellationToken ct = default);
    Task RevokeAsync(Guid reservationId, Guid actorUserId, bool isAdmin, string reason, CancellationToken ct = default);
}
