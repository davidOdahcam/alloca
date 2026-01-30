using Alloca.Domain.Enums;

namespace Alloca.Application.DTOs.Availability;

public record CheckAvailabilityRequest(DateTime StartUtc, DateTime EndUtc);

public record AvailabilityResourceResponse(
    Guid Id,
    string ExternalId,
    string Name,
    ResourceType Type,
    Guid? RoomId,
    bool Available);
