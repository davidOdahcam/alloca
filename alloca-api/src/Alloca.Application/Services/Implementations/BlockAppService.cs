using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Blocks;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Services;
using Alloca.Domain.ValueObjects;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class BlockAppService(
    IBlockService blockService,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IValidator<CreateBlockRequest> validator) : IBlockAppService
{
    public async Task<CreateBlockResponse> CreateAsync(CreateBlockRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var userId = RequireUserId();

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var block = await blockService.CreateAsync(userId, IsAdmin, request.TargetType, request.TargetId, period, request.Reason, ct);
        return new CreateBlockResponse(block.Id);
    }

    public async Task<IReadOnlyList<BlockListItemResponse>> ListAsync(Guid? pavilionId, bool includeExpired, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var views = await blockService.ListAsync(userId, IsAdmin, pavilionId, includeExpired, clock.UtcNow, ct);
        return views.Select(ToResponse).ToList();
    }

    private bool IsAdmin => currentUser.Role == UserRole.Admin;

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");
        return currentUser.UserId.Value;
    }

    private static BlockListItemResponse ToResponse(BlockView b) =>
        new(b.Id, b.TargetType, b.TargetId, b.TargetName, b.PavilionId, b.PavilionName,
            b.StartUtc, b.EndUtc, b.Reason, b.IsActive);
}
