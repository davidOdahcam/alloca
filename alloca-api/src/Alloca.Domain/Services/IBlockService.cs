using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pelas regras de bloqueio de recursos.
/// </summary>
public interface IBlockService
{
    Task<Block> CreateAsync(Guid actorUserId, bool isAdmin, BlockTargetType targetType, Guid targetId, TimeRange period, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<BlockView>> ListAsync(Guid actorUserId, bool isAdmin, Guid? pavilionFilter, bool includeExpired, DateTime nowUtc, CancellationToken ct = default);
}
