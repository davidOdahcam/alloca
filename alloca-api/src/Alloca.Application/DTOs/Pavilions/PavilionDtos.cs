namespace Alloca.Application.DTOs.Pavilions;

public record OperatingHoursResponse(int DayOfWeek, string OpensAt, string ClosesAt);

public record PavilionResponse(
    Guid Id,
    string Code,
    string Name,
    int SlotMinutes,
    int MinAdvanceMinutes,
    int MaxAdvanceDays,
    IReadOnlyList<OperatingHoursResponse> OperatingHours);

public record FloorResponse(Guid Id, string Code, string Name, int Level, string? SvgKey);

public record FloorRoomResource(Guid Id, string ExternalId, string Name, bool IsReservable, IReadOnlyList<FloorDeskResource> Desks);

public record FloorDeskResource(Guid Id, string ExternalId, string Name, bool IsReservable);

public record FloorResourcesResponse(Guid FloorId, IReadOnlyList<FloorRoomResource> Rooms);
