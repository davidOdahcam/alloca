using Alloca.Domain.Enums;

namespace Alloca.Domain.ReadModels;

/// <summary>
/// Projeção de leitura de uma reserva já enriquecida com nomes do recurso e do pavilhão.
/// Produzida pelos repositórios (acesso a dados) e consumida pelos serviços de domínio.
/// </summary>
public record ReservationView(
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
    DateTime CreatedAt);
