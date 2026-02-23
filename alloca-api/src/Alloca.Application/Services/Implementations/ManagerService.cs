using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Manager;
using Alloca.Application.DTOs.Reservations;
using Alloca.Domain.Common;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Services.Implementations;

public class ManagerService(
    IReservationRepository reservations,
    IPavilionRepository pavilions,
    IRoomRepository rooms,
    IDeskRepository desks,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IValidator<ReasonRequest> reasonValidator) : IManagerService
{
    public async Task<IReadOnlyList<ReservationResponse>> ListPendingAsync(Guid? pavilionId, CancellationToken ct = default)
    {
        var userId = RequireUserId();

        List<Guid> managedPavilionIds = currentUser.Role == UserRole.Admin
            ? await pavilions.Query().Select(p => p.Id).ToListAsync(ct)
            : (await pavilions.GetManagedPavilionIdsAsync(userId, ct)).ToList();

        var query =
            from r in reservations.Query()
            where r.Status == ReservationStatus.Pending
                && managedPavilionIds.Contains(r.PavilionId)
                && (pavilionId == null || r.PavilionId == pavilionId)
            join pav in pavilions.Query() on r.PavilionId equals pav.Id
            join room in rooms.Query() on r.RoomId equals room.Id into roomJ
            from room in roomJ.DefaultIfEmpty()
            join desk in desks.Query() on r.DeskId equals desk.Id into deskJ
            from desk in deskJ.DefaultIfEmpty()
            orderby r.CreatedAt
            select new ReservationResponse(
                r.Id, r.ResourceType, r.RoomId, r.DeskId,
                r.ResourceType == ResourceType.Room ? room!.ExternalId : desk!.ExternalId,
                r.ResourceType == ResourceType.Room ? room!.Name : desk!.Name,
                r.PavilionId, pav.Name, r.Period.StartUtc, r.Period.EndUtc,
                r.Status, r.Notes, r.DecisionReason, r.CheckedInAt, r.CreatedAt);

        return await query.ToListAsync(ct);
    }

    public async Task ApproveAsync(Guid reservationId, CancellationToken ct = default)
    {
        var (userId, reservation) = await LoadAndAuthorizeAsync(reservationId, ct);
        try { reservation.Approve(userId); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default)
    {
        await reasonValidator.ValidateAndThrowAsync(request, ct);
        var (userId, reservation) = await LoadAndAuthorizeAsync(reservationId, ct);
        try { reservation.Reject(userId, request.Reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default)
    {
        await reasonValidator.ValidateAndThrowAsync(request, ct);
        var (userId, reservation) = await LoadAndAuthorizeAsync(reservationId, ct);
        try { reservation.Revoke(userId, request.Reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");
        return currentUser.UserId.Value;
    }

    private async Task<(Guid userId, Domain.Entities.Reservation reservation)> LoadAndAuthorizeAsync(Guid reservationId, CancellationToken ct)
    {
        var userId = RequireUserId();
        var reservation = await reservations.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");

        if (currentUser.Role != UserRole.Admin)
        {
            var manages = await pavilions.ManagesAsync(reservation.PavilionId, userId, ct);
            if (!manages) throw new ForbiddenException(ErrorCodes.ManagerNotPavilionManager, "Você não é gestor deste pavilhão.");
        }
        return (userId, reservation);
    }
}
