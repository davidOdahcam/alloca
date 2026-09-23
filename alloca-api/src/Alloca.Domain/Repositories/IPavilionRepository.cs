using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IPavilionRepository : IRepository<Pavilion>
{
    Task<IReadOnlyList<Pavilion>> ListWithOperatingHoursAsync(CancellationToken ct = default);
    Task<Pavilion?> GetWithOperatingHoursAsync(Guid pavilionId, CancellationToken ct = default);
    Task<bool> ManagesAsync(Guid pavilionId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetManagedPavilionIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken ct = default);
}
