using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class RoomRepository(AllocaDbContext db) : Repository<Room>(db), IRoomRepository
{
    public Task<Room?> GetWithDesksAsync(Guid roomId, CancellationToken ct = default)
        => Set.Include(r => r.Desks).FirstOrDefaultAsync(r => r.Id == roomId, ct);
}
