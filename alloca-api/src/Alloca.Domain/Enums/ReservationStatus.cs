namespace Alloca.Domain.Enums;

public enum ReservationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    CancelledByUser = 3,
    RevokedByManager = 4,
    InProgress = 5,
    Completed = 6,
    NoShow = 7
}
