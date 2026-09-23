using Alloca.Domain.Entities;
using Alloca.Domain.ReadModels;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio que expõe consultas de pavilhões, andares, recursos e disponibilidade.
/// </summary>
public interface IPavilionService
{
    Task<IReadOnlyList<Pavilion>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Floor>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default);
    Task<Floor> GetFloorWithResourcesAsync(Guid pavilionId, Guid floorId, CancellationToken ct = default);
    Task<IReadOnlyList<AvailabilityResource>> CheckAvailabilityAsync(Guid pavilionId, Guid floorId, TimeRange period, CancellationToken ct = default);
}
