using Alloca.Application.DTOs.Availability;
using Alloca.Application.DTOs.Pavilions;
using Alloca.Domain.Entities;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Services;
using Alloca.Domain.ValueObjects;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class PavilionAppService(
    IPavilionService pavilionService,
    IValidator<CheckAvailabilityRequest> availabilityValidator) : IPavilionAppService
{
    public async Task<IReadOnlyList<PavilionResponse>> ListAsync(CancellationToken ct = default)
    {
        var list = await pavilionService.ListAsync(ct);
        return list.Select(ToPavilionResponse).ToList();
    }

    public async Task<IReadOnlyList<FloorResponse>> ListFloorsAsync(Guid pavilionId, CancellationToken ct = default)
    {
        var list = await pavilionService.ListFloorsAsync(pavilionId, ct);
        return list.Select(f => new FloorResponse(f.Id, f.Code, f.Name, f.Level, f.SvgKey)).ToList();
    }

    public async Task<FloorResourcesResponse> ListFloorResourcesAsync(Guid pavilionId, Guid floorId, CancellationToken ct = default)
    {
        var floor = await pavilionService.GetFloorWithResourcesAsync(pavilionId, floorId, ct);

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

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var resources = await pavilionService.CheckAvailabilityAsync(pavilionId, floorId, period, ct);
        return resources.Select(ToAvailabilityResponse).ToList();
    }

    private static PavilionResponse ToPavilionResponse(Pavilion p) =>
        new(
            p.Id,
            p.Code,
            p.Name,
            p.SlotMinutes,
            p.MinAdvanceMinutes,
            p.MaxAdvanceDays,
            p.OperatingHours
                .OrderBy(o => o.DayOfWeek)
                .Select(o => new OperatingHoursResponse((int)o.DayOfWeek, o.OpensAt.ToString("HH:mm"), o.ClosesAt.ToString("HH:mm")))
                .ToList());

    private static AvailabilityResourceResponse ToAvailabilityResponse(AvailabilityResource r) =>
        new(r.ResourceId, r.ExternalId, r.Name, r.ResourceType, r.RoomId, r.Available);
}
