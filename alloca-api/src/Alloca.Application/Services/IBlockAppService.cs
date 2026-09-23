using Alloca.Application.DTOs.Blocks;

namespace Alloca.Application.Services;

public interface IBlockAppService
{
    Task<CreateBlockResponse> CreateAsync(CreateBlockRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BlockListItemResponse>> ListAsync(Guid? pavilionId, bool includeExpired, CancellationToken ct = default);
}
