using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio que concentra as regras de negócio das reservas
/// (criação, cancelamento, check-in e consulta) consumindo os repositórios.
/// </summary>
public interface IReservationService
{
    Task<Reservation> CreateAsync(Guid userId, ResourceType resourceType, Guid resourceId, TimeRange period, string? notes, DateTime nowUtc, CancellationToken ct = default);
    Task CancelAsync(Guid reservationId, Guid userId, DateTime nowUtc, CancellationToken ct = default);
    Task CheckInAsync(Guid reservationId, Guid userId, string scannedExternalId, DateTime nowUtc, CancellationToken ct = default);
    Task<string> GetQrPayloadAsync(Guid reservationId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationView>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}
