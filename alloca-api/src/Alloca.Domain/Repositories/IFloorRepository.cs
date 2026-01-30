using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IFloorRepository : IRepository<Floor>
{
    Task<Floor?> GetWithRoomsAndDesksAsync(Guid floorId, Guid pavilionId, CancellationToken ct = default);
    Task<IReadOnlyList<Floor>> ListByPavilionAsync(Guid pavilionId, CancellationToken ct = default);
}
