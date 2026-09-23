using Alloca.Domain.Enums;

namespace Alloca.Domain.ReadModels;

/// <summary>
/// Projeção de leitura de um bloqueio já enriquecida com o nome do alvo e o pavilhão afetado.
/// </summary>
public record BlockView(
    Guid Id,
    BlockTargetType TargetType,
    Guid TargetId,
    string TargetName,
    Guid? PavilionId,
    string? PavilionName,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason,
    bool IsActive);
