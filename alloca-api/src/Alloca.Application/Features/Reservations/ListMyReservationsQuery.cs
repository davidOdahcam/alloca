using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Reservations;

public record ReservationDto(
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

public record ListMyReservationsQuery() : IRequest<IReadOnlyList<ReservationDto>>;

public class ListMyReservationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListMyReservationsQuery, IReadOnlyList<ReservationDto>>
{
    public async Task<IReadOnlyList<ReservationDto>> Handle(ListMyReservationsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null) throw new UnauthorizedException("User not authenticated.");
        var userId = currentUser.UserId.Value;

        var query =
            from r in db.Reservations
            where r.UserId == userId
            join pav in db.Pavilions on r.PavilionId equals pav.Id
            join room in db.Rooms on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in db.Desks on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.Period.StartUtc descending
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
