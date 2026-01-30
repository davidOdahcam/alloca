using Alloca.Application.DTOs.Availability;
using Alloca.Application.DTOs.Pavilions;

namespace Alloca.Application.Services;

public interface IPavilionService
{
    Task<IReadOnlyList<PavilionResponse>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FloorResponse>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default);
    Task<IReadOnlyList<AvailabilityResourceResponse>> CheckAvailabilityAsync(Guid pavilionId, Guid floorId, CheckAvailabilityRequest request, CancellationToken ct = default);
}
