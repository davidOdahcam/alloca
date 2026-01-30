using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Availability;
using Alloca.Application.DTOs.Pavilions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Services.Implementations;

public class PavilionService(
    IPavilionRepository pavilions,
    IFloorRepository floors,
    IBlockRepository blocks,
    IReservationRepository reservations,
    IDateTimeProvider clock,
    IValidator<CheckAvailabilityRequest> availabilityValidator) : IPavilionService
{
    public async Task<IReadOnlyList<PavilionResponse>> ListAsync(CancellationToken ct = default)
    {
        return await pavilions.Query()
            .OrderBy(p => p.Code)
            .Select(p => new PavilionResponse(p.Id, p.Code, p.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FloorResponse>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default)
    {
        var exists = await pavilions.AnyAsync(p => p.Id == pavilionId, ct);
        if (!exists) throw new NotFoundException("Pavilion not found.");

        var list = await floors.ListByPavilionAsync(pavilionId, ct);
        return list.Select(f => new FloorResponse(f.Id, f.Code, f.Name, f.Level, f.SvgKey)).ToList();
    }

    public async Task<IReadOnlyList<AvailabilityResourceResponse>> CheckAvailabilityAsync(
        Guid pavilionId, Guid floorId, CheckAvailabilityRequest request, CancellationToken ct = default)
    {
        await availabilityValidator.ValidateAndThrowAsync(request, ct);

        var pavilion = await pavilions.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException("Pavilion not found.");

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        if (!IsWithinOperatingHours(pavilion, period))
            return [];

        var floor = await floors.GetWithRoomsAndDesksAsync(floorId, pavilionId, ct)
            ?? throw new NotFoundException("Floor not found.");

        var roomList = floor.Rooms.ToList();
        var roomIds = roomList.Select(r => r.Id).ToList();
        var deskIds = roomList.SelectMany(r => r.Desks).Select(d => d.Id).ToList();

        if (await blocks.AnyBlockingAsync(BlockTargetType.Pavilion, pavilionId, startUtc, endUtc, ct))
            return [];

        var blockedRooms = (await blocks.ListBlockedTargetsAsync(BlockTargetType.Room, roomIds, startUtc, endUtc, ct)).ToHashSet();
        var blockedDesks = (await blocks.ListBlockedTargetsAsync(BlockTargetType.Desk, deskIds, startUtc, endUtc, ct)).ToHashSet();
        var busyRooms = (await reservations.ListConflictingRoomIdsAsync(roomIds, startUtc, endUtc, ct)).ToHashSet();
        var busyDesks = (await reservations.ListConflictingDeskIdsAsync(deskIds, startUtc, endUtc, ct)).ToHashSet();

        var result = new List<AvailabilityResourceResponse>();
        foreach (var room in roomList)
        {
            if (room.IsReservable)
            {
                var available = !blockedRooms.Contains(room.Id) && !busyRooms.Contains(room.Id);
                result.Add(new AvailabilityResourceResponse(room.Id, room.ExternalId, room.Name, ResourceType.Room, null, available));
            }
            foreach (var desk in room.Desks)
            {
                var available = !blockedRooms.Contains(room.Id)
                    && !blockedDesks.Contains(desk.Id)
                    && !busyDesks.Contains(desk.Id);
                result.Add(new AvailabilityResourceResponse(desk.Id, desk.ExternalId, desk.Name, ResourceType.Desk, room.Id, available));
            }
        }
        return result;
    }

    private static bool IsWithinOperatingHours(Pavilion pavilion, TimeRange period)
    {
        var startLocal = period.StartUtc.ToLocalTime();
        var endLocal = period.EndUtc.ToLocalTime();
        if (startLocal.Date != endLocal.Date) return false;
        var oh = pavilion.OperatingHours.FirstOrDefault(o => o.DayOfWeek == startLocal.DayOfWeek);
        if (oh is null || oh.IsClosed) return false;
        var start = TimeOnly.FromDateTime(startLocal);
        var end = TimeOnly.FromDateTime(endLocal);
        return start >= oh.OpensAt && end <= oh.ClosesAt;
    }
}
