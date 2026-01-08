using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alloca.Application.Features.Reservations;

public record CancelReservationCommand(Guid ReservationId) : IRequest<Unit>;

public class CancelReservationCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IOptions<ReservationPolicySettings> policyOpts) : IRequestHandler<CancelReservationCommand, Unit>
{
    public async Task<Unit> Handle(CancelReservationCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null) throw new UnauthorizedException("User not authenticated.");
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        if (reservation.UserId != currentUser.UserId.Value)
            throw new ForbiddenException("You can only cancel your own reservations.");

        try
        {
            reservation.CancelByUser(clock.UtcNow, policyOpts.Value.CancellationCutoffHours);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
