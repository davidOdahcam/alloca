using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IRoomRepository : IRepository<Room>
{
    Task<Room?> GetWithDesksAsync(Guid roomId, CancellationToken ct = default);
}
