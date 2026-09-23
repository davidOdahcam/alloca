using Alloca.Domain.Enums;

namespace Alloca.Domain.ReadModels;

/// <summary>
/// Resultado da verificação de disponibilidade de um recurso (sala ou mesa) em um período.
/// </summary>
public record AvailabilityResource(
    Guid ResourceId,
    string ExternalId,
    string Name,
    ResourceType ResourceType,
    Guid? RoomId,
    bool Available);
