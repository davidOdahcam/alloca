using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class UserStrike : Entity
{
    public Guid UserId { get; private set; }
    public Guid ReservationId { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private UserStrike() { }

    public UserStrike(Guid userId, Guid reservationId, DateTime issuedAt, int windowDays)
    {
        UserId = userId;
        ReservationId = reservationId;
        IssuedAt = issuedAt;
        ExpiresAt = issuedAt.AddDays(windowDays);
    }

    public bool IsActive(DateTime nowUtc) => nowUtc < ExpiresAt;
}
