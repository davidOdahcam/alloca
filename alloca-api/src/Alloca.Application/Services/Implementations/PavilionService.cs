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
        var list = await pavilions.Query()
            .Include(p => p.OperatingHours)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

        return list
            .Select(p => new PavilionResponse(
                p.Id,
                p.Code,
                p.Name,
                p.SlotMinutes,
                p.MinAdvanceMinutes,
                p.MaxAdvanceDays,
                p.OperatingHours
                    .OrderBy(o => o.DayOfWeek)
                    .Select(o => new OperatingHoursResponse((int)o.DayOfWeek, o.OpensAt.ToString("HH:mm"), o.ClosesAt.ToString("HH:mm")))
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<FloorResponse>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default)
    {
        var exists = await pavilions.AnyAsync(p => p.Id == pavilionId, ct);
        if (!exists) throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var list = await floors.ListByPavilionAsync(pavilionId, ct);
        return list.Select(f => new FloorResponse(f.Id, f.Code, f.Name, f.Level, f.SvgKey)).ToList();
    }

    public async Task<FloorResourcesResponse> ListFloorResourcesAsync(Guid pavilionId, Guid floorId, CancellationToken ct = default)
    {
        var exists = await pavilions.AnyAsync(p => p.Id == pavilionId, ct);
        if (!exists) throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var floor = await floors.GetWithRoomsAndDesksAsync(floorId, pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");

        var rooms = floor.Rooms
            .OrderBy(r => r.ExternalId)
            .Select(r => new FloorRoomResource(
                r.Id,
                r.ExternalId,
                r.Name,
                r.IsReservable,
                r.Desks
                    .OrderBy(d => d.ExternalId)
                    .Select(d => new FloorDeskResource(d.Id, d.ExternalId, d.Name, d.IsReservable))
                    .ToList()))
            .ToList();

        return new FloorResourcesResponse(floor.Id, rooms);
    }

    public async Task<IReadOnlyList<AvailabilityResourceResponse>> CheckAvailabilityAsync(
        Guid pavilionId, Guid floorId, CheckAvailabilityRequest request, CancellationToken ct = default)
    {
        await availabilityValidator.ValidateAndThrowAsync(request, ct);

        var pavilion = await pavilions.GetWithOperatingHoursAsync(pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.PavilionNotFound, "Pavilhão não encontrado.");

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        if (!IsWithinOperatingHours(pavilion, period))
            return [];

        var floor = await floors.GetWithRoomsAndDesksAsync(floorId, pavilionId, ct)
            ?? throw new NotFoundException(ErrorCodes.FloorNotFound, "Andar não encontrado.");

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
            var anyDeskBusy = room.Desks.Any(d => busyDesks.Contains(d.Id));
            var roomItselfBusy = busyRooms.Contains(room.Id);
            if (room.IsReservable)
            {
                // A sala só está disponível se ela mesma não estiver reservada
                // e se nenhuma de suas mesas estiver reservada no período.
                var available = !blockedRooms.Contains(room.Id) && !roomItselfBusy && !anyDeskBusy;
                result.Add(new AvailabilityResourceResponse(room.Id, room.ExternalId, room.Name, ResourceType.Room, null, available));
            }
            foreach (var desk in room.Desks)
            {
                if (!desk.IsReservable) continue;
                // A mesa não está disponível se a sala pai estiver reservada no período.
                var available = !blockedRooms.Contains(room.Id)
                    && !blockedDesks.Contains(desk.Id)
                    && !busyDesks.Contains(desk.Id)
                    && !roomItselfBusy;
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
