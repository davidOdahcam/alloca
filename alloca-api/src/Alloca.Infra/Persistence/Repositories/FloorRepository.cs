using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class FloorRepository(AllocaDbContext db) : Repository<Floor>(db), IFloorRepository
{
    public Task<Floor?> GetWithRoomsAndDesksAsync(Guid floorId, Guid pavilionId, CancellationToken ct = default)
        => Set.Include(f => f.Rooms).ThenInclude(r => r.Desks)
              .AsSplitQuery()
              .FirstOrDefaultAsync(f => f.Id == floorId && f.PavilionId == pavilionId, ct);

    public async Task<IReadOnlyList<Floor>> ListByPavilionAsync(Guid pavilionId, CancellationToken ct = default)
        => await Set.Where(f => f.PavilionId == pavilionId).OrderBy(f => f.Level).ToListAsync(ct);
}
