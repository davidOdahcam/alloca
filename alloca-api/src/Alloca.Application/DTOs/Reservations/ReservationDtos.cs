using Alloca.Domain.Enums;

namespace Alloca.Application.DTOs.Reservations;

public record CreateReservationRequest(
    ResourceType ResourceType,
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes);

public record CreateReservationResponse(Guid Id);

public record CheckInRequest(string ScannedExternalId);

public record ReservationResponse(
    Guid Id,
    ResourceType ResourceType,
    Guid? RoomId,
    Guid? DeskId,
    string ResourceExternalId,
    string ResourceName,
    Guid PavilionId,
    string PavilionName,
    DateTime StartUtc,
    DateTime EndUtc,
    ReservationStatus Status,
    string? Notes,
    string? DecisionReason,
    DateTime? CheckedInAt,
    DateTime CreatedAt);
