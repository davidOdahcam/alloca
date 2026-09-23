using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Services.Implementations;

public class BlockService(
    IBlockRepository blockRepository,
    IPavilionRepository pavilionRepository,
    IRoomRepository roomRepository,
    IDeskRepository deskRepository,
    IFloorRepository floorRepository,
    IUnitOfWork uow) : IBlockService
{
    public async Task<Block> CreateAsync(Guid actorUserId, bool isAdmin, BlockTargetType targetType, Guid targetId, TimeRange period, string reason, CancellationToken ct = default)
    {
        var pavilionId = await ResolvePavilionIdAsync(targetType, targetId, ct);
        if (!isAdmin)
        {
            var manages = await pavilionRepository.ManagesAsync(pavilionId, actorUserId, ct);
            if (!manages) throw new ForbiddenException(ErrorCodes.BlockNotPavilionManager, "Você não é gestor do pavilhão afetado.");
        }

        var block = new Block(targetType, targetId, period, reason, actorUserId);
        blockRepository.Add(block);
        await uow.SaveChangesAsync(ct);
        return block;
    }

    public async Task<IReadOnlyList<BlockView>> ListAsync(Guid actorUserId, bool isAdmin, Guid? pavilionFilter, bool includeExpired, DateTime nowUtc, CancellationToken ct = default)
    {
        HashSet<Guid>? managedIds = null;
        if (!isAdmin)
            managedIds = (await pavilionRepository.GetManagedPavilionIdsAsync(actorUserId, ct)).ToHashSet();

        var all = await blockRepository.ListWithDetailsAsync(includeExpired, nowUtc, ct);

        var result = new List<BlockView>(all.Count);
        foreach (var b in all)
        {
            if (managedIds is not null && (b.PavilionId is null || !managedIds.Contains(b.PavilionId.Value)))
                continue;

            if (pavilionFilter.HasValue && pavilionFilter.Value != b.PavilionId)
                continue;

            result.Add(b);
        }
        return result;
    }

    private async Task<Guid> ResolvePavilionIdAsync(BlockTargetType type, Guid targetId, CancellationToken ct)
    {
        switch (type)
        {
            case BlockTargetType.Pavilion:
                if (!await pavilionRepository.AnyAsync(p => p.Id == targetId, ct))
                    throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");
                return targetId;
            case BlockTargetType.Room:
                var room = await roomRepository.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
                var floor = await floorRepository.GetByIdAsync(room.FloorId, ct)
                    ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
                return floor.PavilionId;
            case BlockTargetType.Desk:
                var desk = await deskRepository.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
                var deskRoom = await roomRepository.GetByIdAsync(desk.RoomId, ct)
                    ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
                var deskFloor = await floorRepository.GetByIdAsync(deskRoom.FloorId, ct)
                    ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
                return deskFloor.PavilionId;
            default:
                throw new BusinessRuleException(ErrorCodes.BlockTargetUnknown, "Tipo de destino desconhecido.");
        }
    }
}
