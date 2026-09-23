using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Services.Implementations;

public class PavilionService(
    IPavilionRepository pavilionRepository,
    IFloorRepository floorRepository,
    IBlockRepository blockRepository,
    IReservationRepository reservationRepository) : IPavilionService
{
    public Task<IReadOnlyList<Pavilion>> ListAsync(CancellationToken ct = default)
        => pavilionRepository.ListWithOperatingHoursAsync(ct);

    public async Task<IReadOnlyList<Floor>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default)
    {
        await EnsurePavilionExistsAsync(pavilionId, ct);
        return await floorRepository.ListByPavilionAsync(pavilionId, ct);
    }

    public async Task<Floor> GetFloorWithResourcesAsync(Guid pavilionId, Guid floorId, CancellationToken ct = default)
    {
        await EnsurePavilionExistsAsync(pavilionId, ct);
        return await floorRepository.GetWithRoomsAndDesksAsync(floorId, pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");
    }

    public async Task<IReadOnlyList<AvailabilityResource>> CheckAvailabilityAsync(Guid pavilionId, Guid floorId, TimeRange period, CancellationToken ct = default)
    {
        var pavilion = await pavilionRepository.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var startUtc = period.StartUtc;
        var endUtc = period.EndUtc;

        if (!pavilion.IsWithinOperatingHours(period))
            return [];

        var floor = await floorRepository.GetWithRoomsAndDesksAsync(floorId, pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");

        var roomList = floor.Rooms.ToList();
        var roomIds = roomList.Select(r => r.Id).ToList();
        var deskIds = roomList.SelectMany(r => r.Desks).Select(d => d.Id).ToList();

        if (await blockRepository.AnyBlockingAsync(BlockTargetType.Pavilion, pavilionId, startUtc, endUtc, ct))
            return [];

        var blockedRooms = (await blockRepository.ListBlockedTargetsAsync(BlockTargetType.Room, roomIds, startUtc, endUtc, ct)).ToHashSet();
        var blockedDesks = (await blockRepository.ListBlockedTargetsAsync(BlockTargetType.Desk, deskIds, startUtc, endUtc, ct)).ToHashSet();
        var busyRooms = (await reservationRepository.ListConflictingRoomIdsAsync(roomIds, startUtc, endUtc, ct)).ToHashSet();
        var busyDesks = (await reservationRepository.ListConflictingDeskIdsAsync(deskIds, startUtc, endUtc, ct)).ToHashSet();

        var result = new List<AvailabilityResource>();
        foreach (var room in roomList)
        {
            var anyDeskBusy = room.Desks.Any(d => busyDesks.Contains(d.Id));
            var roomItselfBusy = busyRooms.Contains(room.Id);
            if (room.IsReservable)
            {
                // A sala só está disponível se ela mesma não estiver reservada
                // e se nenhuma de suas mesas estiver reservada no período.
                var available = !blockedRooms.Contains(room.Id) && !roomItselfBusy && !anyDeskBusy;
                result.Add(new AvailabilityResource(room.Id, room.ExternalId, room.Name, ResourceType.Room, null, available));
            }
            foreach (var desk in room.Desks)
            {
                if (!desk.IsReservable) continue;
                // A mesa não está disponível se a sala pai estiver reservada no período.
                var available = !blockedRooms.Contains(room.Id)
                    && !blockedDesks.Contains(desk.Id)
                    && !busyDesks.Contains(desk.Id)
                    && !roomItselfBusy;
                result.Add(new AvailabilityResource(desk.Id, desk.ExternalId, desk.Name, ResourceType.Desk, room.Id, available));
            }
        }
        return result;
    }

    private async Task EnsurePavilionExistsAsync(Guid pavilionId, CancellationToken ct)
    {
        if (!await pavilionRepository.AnyAsync(p => p.Id == pavilionId, ct))
            throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");
    }
}
