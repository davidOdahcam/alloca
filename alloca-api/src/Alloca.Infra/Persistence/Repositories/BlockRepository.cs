using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
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

    public async Task<IReadOnlyList<BlockView>> ListWithDetailsAsync(bool includeExpired, DateTime nowUtc, CancellationToken ct = default)
    {
        var query = Set.AsQueryable();
        if (!includeExpired)
            query = query.Where(b => b.Period.EndUtc > nowUtc);

        var list = await query.OrderByDescending(b => b.Period.StartUtc).ToListAsync(ct);
        if (list.Count == 0) return Array.Empty<BlockView>();

        var pavIds = list.Where(b => b.TargetType == BlockTargetType.Pavilion).Select(b => b.TargetId).ToHashSet();
        var roomIds = list.Where(b => b.TargetType == BlockTargetType.Room).Select(b => b.TargetId).ToHashSet();
        var deskIds = list.Where(b => b.TargetType == BlockTargetType.Desk).Select(b => b.TargetId).ToHashSet();

        var roomById = roomIds.Count == 0
            ? new Dictionary<Guid, (string Name, string ExternalId, Guid FloorId)>()
            : await Db.Rooms.Where(r => roomIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.ExternalId, r.FloorId })
                .ToDictionaryAsync(r => r.Id, r => (r.Name, r.ExternalId, r.FloorId), ct);

        var deskById = deskIds.Count == 0
            ? new Dictionary<Guid, (string Name, string ExternalId, Guid RoomId)>()
            : await Db.Desks.Where(d => deskIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name, d.ExternalId, d.RoomId })
                .ToDictionaryAsync(d => d.Id, d => (d.Name, d.ExternalId, d.RoomId), ct);

        // Carrega salas das mesas (para descobrir pavilhão), se ainda não carregadas
        var extraRoomIds = deskById.Values.Select(d => d.RoomId).Where(id => !roomById.ContainsKey(id)).Distinct().ToList();
        if (extraRoomIds.Count > 0)
        {
            var extra = await Db.Rooms.Where(r => extraRoomIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.ExternalId, r.FloorId })
                .ToListAsync(ct);
            foreach (var r in extra)
                roomById[r.Id] = (r.Name, r.ExternalId, r.FloorId);
        }

        var floorIds = roomById.Values.Select(r => r.FloorId).Distinct().ToList();
        var floorToPavilion = floorIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await Db.Floors.Where(f => floorIds.Contains(f.Id))
                .Select(f => new { f.Id, f.PavilionId })
                .ToDictionaryAsync(f => f.Id, f => f.PavilionId, ct);

        var allPavilionIds = new HashSet<Guid>(pavIds);
        foreach (var pid in floorToPavilion.Values) allPavilionIds.Add(pid);

        var pavilionById = allPavilionIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await Db.Pavilions.Where(p => allPavilionIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var result = new List<BlockView>(list.Count);
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

            string? pavilionName = blockPavilionId.HasValue && pavilionById.TryGetValue(blockPavilionId.Value, out var pname)
                ? pname
                : null;

            result.Add(new BlockView(
                b.Id,
                b.TargetType,
                b.TargetId,
                targetName,
                blockPavilionId,
                pavilionName,
                b.Period.StartUtc,
                b.Period.EndUtc,
                b.Reason,
                b.Period.EndUtc > nowUtc));
        }
        return result;
    }
}
