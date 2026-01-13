using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Common;
using Alloca.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Manager;

public record ApproveReservationCommand(Guid ReservationId) : IRequest<Unit>;
public record RejectReservationCommand(Guid ReservationId, string Reason) : IRequest<Unit>;
public record RevokeReservationCommand(Guid ReservationId, string Reason) : IRequest<Unit>;

public class RejectValidator : AbstractValidator<RejectReservationCommand>
{
    public RejectValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public class RevokeValidator : AbstractValidator<RevokeReservationCommand>
{
    public RevokeValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

internal static class ManagerGuard
{
    public static async Task EnsureManagesPavilionAsync(IAppDbContext db, ICurrentUserService current, Guid pavilionId, CancellationToken ct)
    {
        if (current.UserId is null) throw new UnauthorizedException("Not authenticated.");
        if (current.Role == UserRole.Admin) return;
        var manages = await db.PavilionManagers.AnyAsync(m =>
            m.PavilionId == pavilionId && m.UserId == current.UserId.Value, ct);
        if (!manages) throw new ForbiddenException("User does not manage this pavilion.");
    }
}

public class ApproveReservationHandler(IAppDbContext db, ICurrentUserService current)
    : IRequestHandler<ApproveReservationCommand, Unit>
{
    public async Task<Unit> Handle(ApproveReservationCommand request, CancellationToken ct)
    {
        var r = await db.Reservations.FirstOrDefaultAsync(x => x.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        await ManagerGuard.EnsureManagesPavilionAsync(db, current, r.PavilionId, ct);
        try { r.Approve(current.UserId!.Value); }
        catch (DomainException ex) { throw new BusinessRuleException(ex.Message); }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class RejectReservationHandler(IAppDbContext db, ICurrentUserService current)
    : IRequestHandler<RejectReservationCommand, Unit>
{
    public async Task<Unit> Handle(RejectReservationCommand request, CancellationToken ct)
    {
        var r = await db.Reservations.FirstOrDefaultAsync(x => x.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        await ManagerGuard.EnsureManagesPavilionAsync(db, current, r.PavilionId, ct);
        try { r.Reject(current.UserId!.Value, request.Reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ex.Message); }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class RevokeReservationHandler(IAppDbContext db, ICurrentUserService current)
    : IRequestHandler<RevokeReservationCommand, Unit>
{
    public async Task<Unit> Handle(RevokeReservationCommand request, CancellationToken ct)
    {
        var r = await db.Reservations.FirstOrDefaultAsync(x => x.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        await ManagerGuard.EnsureManagesPavilionAsync(db, current, r.PavilionId, ct);
        try { r.Revoke(current.UserId!.Value, request.Reason); }
        catch (DomainException ex) { throw new BusinessRuleException(ex.Message); }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
