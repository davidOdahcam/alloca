using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Pavilions;

public record FloorDto(Guid Id, string Code, string Name, int Level, string? SvgKey);

public record ListFloorsQuery(Guid PavilionId) : IRequest<IReadOnlyList<FloorDto>>;

public class ListFloorsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListFloorsQuery, IReadOnlyList<FloorDto>>
{
    public async Task<IReadOnlyList<FloorDto>> Handle(ListFloorsQuery request, CancellationToken ct)
    {
        var exists = await db.Pavilions.AnyAsync(p => p.Id == request.PavilionId, ct);
        if (!exists) throw new NotFoundException("Pavilion not found.");

        return await db.Floors
            .Where(f => f.PavilionId == request.PavilionId)
            .OrderBy(f => f.Level)
            .Select(f => new FloorDto(f.Id, f.Code, f.Name, f.Level, f.SvgKey))
            .ToListAsync(ct);
    }
}
