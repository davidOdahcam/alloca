using Alloca.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Pavilions;

public record PavilionDto(Guid Id, string Code, string Name);

public record ListPavilionsQuery() : IRequest<IReadOnlyList<PavilionDto>>;

public class ListPavilionsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListPavilionsQuery, IReadOnlyList<PavilionDto>>
{
    public async Task<IReadOnlyList<PavilionDto>> Handle(ListPavilionsQuery request, CancellationToken ct)
    {
        return await db.Pavilions
            .OrderBy(p => p.Code)
            .Select(p => new PavilionDto(p.Id, p.Code, p.Name))
            .ToListAsync(ct);
    }
}
