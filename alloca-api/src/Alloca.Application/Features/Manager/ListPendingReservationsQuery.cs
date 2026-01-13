using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.Features.Reservations;
using Alloca.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Manager;

public record ListPendingReservationsQuery(Guid? PavilionId) : IRequest<IReadOnlyList<ReservationDto>>;

public class ListPendingReservationsHandler(IAppDbContext db, ICurrentUserService current)
    : IRequestHandler<ListPendingReservationsQuery, IReadOnlyList<ReservationDto>>
{
    public async Task<IReadOnlyList<ReservationDto>> Handle(ListPendingReservationsQuery request, CancellationToken ct)
    {
        if (current.UserId is null) throw new UnauthorizedException("Not authenticated.");
        var userId = current.UserId.Value;

        IQueryable<Guid> managedPavilionIds = current.Role == UserRole.Admin
            ? db.Pavilions.Select(p => p.Id)
            : db.PavilionManagers.Where(m => m.UserId == userId).Select(m => m.PavilionId);

        var query =
            from r in db.Reservations
            where r.Status == ReservationStatus.Pending
                && managedPavilionIds.Contains(r.PavilionId)
                && (request.PavilionId == null || r.PavilionId == request.PavilionId)
            join pav in db.Pavilions on r.PavilionId equals pav.Id
            join room in db.Rooms on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in db.Desks on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.CreatedAt
            select new ReservationDto(
                r.Id,
                r.ResourceType,
                r.RoomId,
                r.DeskId,
                r.ResourceType == ResourceType.Room ? room!.ExternalId : desk!.ExternalId,
                r.ResourceType == ResourceType.Room ? room!.Name : desk!.Name,
                r.PavilionId,
                pav.Name,
                r.Period.StartUtc,
                r.Period.EndUtc,
                r.Status,
                r.Notes,
                r.DecisionReason,
                r.CheckedInAt,
                r.CreatedAt);

        return await query.ToListAsync(ct);
    }
}
