using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class BlockRepository(AllocaDbContext db) : Repository<Block>(db), IBlockRepository
{
    public Task<bool> AnyBlockingAsync(BlockTargetType type, Guid targetId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
        => Set.AnyAsync(b =>
            b.TargetType == type && b.TargetId == targetId
            && b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc, ct);

    public async Task<IReadOnlyList<Guid>> ListBlockedTargetsAsync(BlockTargetType type, IEnumerable<Guid> targetIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var ids = targetIds.ToHashSet();
        return await Set.Where(b =>
            b.TargetType == type && ids.Contains(b.TargetId)
            && b.Period.StartUtc < endUtc && startUtc < b.Period.EndUtc)
            .Select(b => b.TargetId).ToListAsync(ct);
    }
}
