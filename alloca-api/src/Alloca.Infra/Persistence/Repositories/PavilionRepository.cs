using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class PavilionRepository(AllocaDbContext db) : Repository<Pavilion>(db), IPavilionRepository
{
    public async Task<IReadOnlyList<Pavilion>> ListWithOperatingHoursAsync(CancellationToken ct = default)
        => await Set.Include(p => p.OperatingHours).OrderBy(p => p.Code).ToListAsync(ct);

    public Task<Pavilion?> GetWithOperatingHoursAsync(Guid pavilionId, CancellationToken ct = default)
        => Set.Include(p => p.OperatingHours).FirstOrDefaultAsync(p => p.Id == pavilionId, ct);

    public Task<bool> ManagesAsync(Guid pavilionId, Guid userId, CancellationToken ct = default)
        => Db.PavilionManagers.AnyAsync(m => m.PavilionId == pavilionId && m.UserId == userId, ct);

    public async Task<IReadOnlyList<Guid>> GetManagedPavilionIdsAsync(Guid userId, CancellationToken ct = default)
        => await Db.PavilionManagers.Where(m => m.UserId == userId).Select(m => m.PavilionId).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken ct = default)
        => await Set.Select(p => p.Id).ToListAsync(ct);
}
