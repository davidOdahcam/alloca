using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;

namespace Alloca.Domain.Repositories;

public interface IBlockRepository : IRepository<Block>
{
    Task<bool> AnyBlockingAsync(BlockTargetType type, Guid targetId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListBlockedTargetsAsync(BlockTargetType type, IEnumerable<Guid> targetIds, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<BlockView>> ListWithDetailsAsync(bool includeExpired, DateTime nowUtc, CancellationToken ct = default);
}
