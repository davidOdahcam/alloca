using Alloca.Domain.Enums;

namespace Alloca.Application.DTOs.Blocks;

public record CreateBlockRequest(
    BlockTargetType TargetType,
    Guid TargetId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason);

public record CreateBlockResponse(Guid Id);

public record BlockListItemResponse(
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
