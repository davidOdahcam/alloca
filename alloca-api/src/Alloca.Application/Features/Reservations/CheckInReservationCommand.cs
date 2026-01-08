using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.Common.Settings;
using Alloca.Domain.Common;
using Alloca.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alloca.Application.Features.Reservations;

/// <summary>
/// Performs check-in by validating that the scanned external resource ID matches the reservation's resource.
/// </summary>
public record CheckInReservationCommand(Guid ReservationId, string ScannedExternalId) : IRequest<Unit>;

public class CheckInReservationCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IOptions<ReservationPolicySettings> policyOpts) : IRequestHandler<CheckInReservationCommand, Unit>
{
    public async Task<Unit> Handle(CheckInReservationCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null) throw new UnauthorizedException("User not authenticated.");
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation not found.");
        if (reservation.UserId != currentUser.UserId.Value)
            throw new ForbiddenException("You can only check-in your own reservations.");

        var scanned = (request.ScannedExternalId ?? string.Empty).Trim().ToUpperInvariant();
        string expected;
        if (reservation.ResourceType == ResourceType.Room)
        {
            expected = (await db.Rooms.FirstAsync(r => r.Id == reservation.RoomId, ct)).ExternalId;
        }
        else
        {
            expected = (await db.Desks.FirstAsync(d => d.Id == reservation.DeskId, ct)).ExternalId;
        }
        if (scanned != expected)
            throw new BusinessRuleException("Scanned QR does not match the reserved resource.");

        try
        {
            reservation.CheckIn(clock.UtcNow, policyOpts.Value.NoShowGraceMinutes);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
