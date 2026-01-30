namespace Alloca.Application.DTOs.Pavilions;

public record PavilionResponse(Guid Id, string Code, string Name);

public record FloorResponse(Guid Id, string Code, string Name, int Level, string? SvgKey);
