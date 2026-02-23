using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Blocks;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Services.Implementations;

public class BlockService(
    IBlockRepository blocks,
    IPavilionRepository pavilions,
    IRoomRepository rooms,
    IDeskRepository desks,
    IFloorRepository floors,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IValidator<CreateBlockRequest> validator) : IBlockService
{
    public async Task<CreateBlockResponse> CreateAsync(CreateBlockRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");

        var pavilionId = await ResolvePavilionIdAsync(request.TargetType, request.TargetId, ct);
        if (currentUser.Role != UserRole.Admin)
        {
            var manages = await pavilions.ManagesAsync(pavilionId, currentUser.UserId.Value, ct);
            if (!manages) throw new ForbiddenException(ErrorCodes.BlockNotPavilionManager, "Você não é gestor do pavilhão afetado.");
        }

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var block = new Block(request.TargetType, request.TargetId, period, request.Reason, currentUser.UserId.Value);
        blocks.Add(block);
        await uow.SaveChangesAsync(ct);
        return new CreateBlockResponse(block.Id);
    }

    private async Task<Guid> ResolvePavilionIdAsync(BlockTargetType type, Guid targetId, CancellationToken ct)
    {
        switch (type)
        {
            case BlockTargetType.Pavilion:
                if (!await pavilions.AnyAsync(p => p.Id == targetId, ct))
                    throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");
                return targetId;
            case BlockTargetType.Room:
                var room = await rooms.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
                var floor = await floors.GetByIdAsync(room.FloorId, ct)
                    ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
                return floor.PavilionId;
            case BlockTargetType.Desk:
                var desk = await desks.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException(ErrorCodes.DeskNotFound, "Mesa não encontrada.");
                var deskRoom = await rooms.GetByIdAsync(desk.RoomId, ct)
                    ?? throw new NotFoundException(ErrorCodes.RoomNotFound, "Sala não encontrada.");
                var deskFloor = await floors.GetByIdAsync(deskRoom.FloorId, ct)
                    ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
                return deskFloor.PavilionId;
            default:
                throw new BusinessRuleException(ErrorCodes.BlockTargetUnknown, "Tipo de destino desconhecido.");
        }
    }

    public async Task<IReadOnlyList<BlockListItemResponse>> ListAsync(Guid? pavilionFilter, bool includeExpired, CancellationToken ct = default)
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");

        HashSet<Guid>? managedIds = null;
        if (currentUser.Role != UserRole.Admin)
        {
            managedIds = (await pavilions.GetManagedPavilionIdsAsync(currentUser.UserId.Value, ct)).ToHashSet();
        }

        var now = DateTime.UtcNow;
        var query = blocks.Query();
        if (!includeExpired)
            query = query.Where(b => b.Period.EndUtc > now);

        var list = await query.OrderByDescending(b => b.Period.StartUtc).ToListAsync(ct);
        if (list.Count == 0) return Array.Empty<BlockListItemResponse>();

        var pavIds = list.Where(b => b.TargetType == BlockTargetType.Pavilion).Select(b => b.TargetId).ToHashSet();
        var roomIds = list.Where(b => b.TargetType == BlockTargetType.Room).Select(b => b.TargetId).ToHashSet();
        var deskIds = list.Where(b => b.TargetType == BlockTargetType.Desk).Select(b => b.TargetId).ToHashSet();

        var roomById = roomIds.Count == 0
            ? new Dictionary<Guid, (string Name, string ExternalId, Guid FloorId)>()
            : await rooms.Query().Where(r => roomIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.ExternalId, r.FloorId })
                .ToDictionaryAsync(r => r.Id, r => (r.Name, r.ExternalId, r.FloorId), ct);

        var deskById = deskIds.Count == 0
            ? new Dictionary<Guid, (string Name, string ExternalId, Guid RoomId)>()
            : await desks.Query().Where(d => deskIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name, d.ExternalId, d.RoomId })
                .ToDictionaryAsync(d => d.Id, d => (d.Name, d.ExternalId, d.RoomId), ct);

        // Carrega salas das mesas (para descobrir pavilhão), se ainda não carregadas
        var extraRoomIds = deskById.Values.Select(d => d.RoomId).Where(id => !roomById.ContainsKey(id)).Distinct().ToList();
        if (extraRoomIds.Count > 0)
        {
            var extra = await rooms.Query().Where(r => extraRoomIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.ExternalId, r.FloorId })
                .ToListAsync(ct);
            foreach (var r in extra)
                roomById[r.Id] = (r.Name, r.ExternalId, r.FloorId);
        }

        var floorIds = roomById.Values.Select(r => r.FloorId).Distinct().ToList();
        var floorToPavilion = floorIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await floors.Query().Where(f => floorIds.Contains(f.Id))
                .Select(f => new { f.Id, f.PavilionId })
                .ToDictionaryAsync(f => f.Id, f => f.PavilionId, ct);

        var allPavilionIds = new HashSet<Guid>(pavIds);
        foreach (var pid in floorToPavilion.Values) allPavilionIds.Add(pid);

        var pavilionById = allPavilionIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await pavilions.Query().Where(p => allPavilionIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var result = new List<BlockListItemResponse>(list.Count);
        foreach (var b in list)
        {
            string targetName = b.TargetId.ToString();
            Guid? blockPavilionId = null;

            switch (b.TargetType)
            {
                case BlockTargetType.Pavilion:
                    blockPavilionId = b.TargetId;
                    if (pavilionById.TryGetValue(b.TargetId, out var pn)) targetName = pn;
                    break;
                case BlockTargetType.Room:
                    if (roomById.TryGetValue(b.TargetId, out var r))
                    {
                        targetName = $"{r.Name} ({r.ExternalId})";
                        if (floorToPavilion.TryGetValue(r.FloorId, out var pid)) blockPavilionId = pid;
                    }
                    break;
                case BlockTargetType.Desk:
                    if (deskById.TryGetValue(b.TargetId, out var d))
                    {
                        targetName = $"{d.Name} ({d.ExternalId})";
                        if (roomById.TryGetValue(d.RoomId, out var dr)
                            && floorToPavilion.TryGetValue(dr.FloorId, out var pid2))
                            blockPavilionId = pid2;
                    }
                    break;
            }

            if (managedIds is not null)
            {
                if (blockPavilionId is null || !managedIds.Contains(blockPavilionId.Value))
                    continue;
            }

            if (pavilionFilter.HasValue && pavilionFilter.Value != blockPavilionId)
                continue;

            string? pavilionName = blockPavilionId.HasValue && pavilionById.TryGetValue(blockPavilionId.Value, out var pname)
                ? pname
                : null;

            result.Add(new BlockListItemResponse(
                b.Id,
                b.TargetType,
                b.TargetId,
                targetName,
                blockPavilionId,
                pavilionName,
                b.Period.StartUtc,
                b.Period.EndUtc,
                b.Reason,
                b.Period.EndUtc > now));
        }
        return result;
    }
}
