using Alloca.Domain.Common;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Repositories;

namespace Alloca.Domain.Services.Implementations;

public class ManagerService(
    IReservationRepository reservationRepository,
    IPavilionRepository pavilionRepository,
    IUnitOfWork uow) : IManagerService
{
    public async Task<IReadOnlyList<ReservationView>> ListPendingAsync(Guid actorUserId, bool isAdmin, Guid? pavilionId, CancellationToken ct = default)
    {
        var managedPavilionIds = isAdmin
            ? await pavilionRepository.GetAllIdsAsync(ct)
            : await pavilionRepository.GetManagedPavilionIdsAsync(actorUserId, ct);

        return await reservationRepository.ListPendingByPavilionsAsync(managedPavilionIds, pavilionId, ct);
    }

    public async Task ApproveAsync(Guid reservationId, Guid actorUserId, bool isAdmin, CancellationToken ct = default)
    {
        var reservation = await LoadAndAuthorizeAsync(reservationId, actorUserId, isAdmin, ct);
        try { reservation.Approve(actorUserId); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid reservationId, Guid actorUserId, bool isAdmin, string reason, CancellationToken ct = default)
    {
        var reservation = await LoadAndAuthorizeAsync(reservationId, actorUserId, isAdmin, ct);
        try { reservation.Reject(actorUserId, reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid reservationId, Guid actorUserId, bool isAdmin, string reason, CancellationToken ct = default)
    {
        var reservation = await LoadAndAuthorizeAsync(reservationId, actorUserId, isAdmin, ct);
        try { reservation.Revoke(actorUserId, reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ErrorCodes.ReservationBusinessRule, ex.Message); }
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Reservation> LoadAndAuthorizeAsync(Guid reservationId, Guid actorUserId, bool isAdmin, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, ct)
            ?? throw new NotFoundException(ErrorCodes.ReservationNotFound, "Reserva não encontrada.");

        if (!isAdmin)
        {
            var manages = await pavilionRepository.ManagesAsync(reservation.PavilionId, actorUserId, ct);
            if (!manages) throw new ForbiddenException(ErrorCodes.ManagerNotPavilionManager, "Você não é gestor deste pavilhão.");
        }
        return reservation;
    }
}
