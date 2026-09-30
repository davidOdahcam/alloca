using Alloca.Domain.Common;
using Alloca.Domain.Enums;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Entities;

public class Reservation : Entity
{
    public Guid UserId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid? RoomId { get; private set; }
    public Guid? DeskId { get; private set; }
    public Guid PavilionId { get; private set; }
    public TimeRange Period { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public DateTime? DecidedAt { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public string? DecisionReason { get; private set; }

    private Reservation() { }

    private Reservation(Guid userId, Guid pavilionId, ResourceType type, Guid? roomId, Guid? deskId, TimeRange period, string? notes)
    {
        UserId = userId;
        PavilionId = pavilionId;
        ResourceType = type;
        RoomId = roomId;
        DeskId = deskId;
        Period = period;
        Notes = notes;
        Status = ReservationStatus.Pending;
    }

    public static Reservation ForRoom(Guid userId, Guid pavilionId, Guid roomId, TimeRange period, string? notes)
        => new(userId, pavilionId, ResourceType.Room, roomId, null, period, notes);

    public static Reservation ForDesk(Guid userId, Guid pavilionId, Guid deskId, TimeRange period, string? notes)
        => new(userId, pavilionId, ResourceType.Desk, null, deskId, period, notes);

    public bool IsActive =>
        Status is ReservationStatus.Pending or ReservationStatus.Approved;

    public void Approve(Guid managerUserId)
    {
        EnsureStatus(ReservationStatus.Pending);
        Status = ReservationStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = managerUserId;
        Touch();
    }

    public void Reject(Guid managerUserId, string reason)
    {
        EnsureStatus(ReservationStatus.Pending);
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Informe o motivo da rejeição.");
        Status = ReservationStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = managerUserId;
        DecisionReason = reason.Trim();
        Touch();
    }

    public void Revoke(Guid managerUserId, string reason)
    {
        if (Status is not ReservationStatus.Approved)
            throw new DomainException("Apenas reservas aprovadas podem ser revogadas.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Informe o motivo da revogação.");
        Status = ReservationStatus.RevokedByManager;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = managerUserId;
        DecisionReason = reason.Trim();
        Touch();
    }

    public void CancelByUser(DateTime nowUtc, int minHoursBeforeStart)
    {
        if (Status == ReservationStatus.Pending)
        {
            Status = ReservationStatus.CancelledByUser;
            Touch();
            return;
        }
        if (Status != ReservationStatus.Approved)
            throw new DomainException("Apenas reservas pendentes ou aprovadas podem ser canceladas.");
        if (Period.StartUtc - nowUtc < TimeSpan.FromHours(minHoursBeforeStart))
            throw new DomainException($"Prazo para cancelamento encerrado ({minHoursBeforeStart}h antes do início).");
        Status = ReservationStatus.CancelledByUser;
        Touch();
    }

    private void EnsureStatus(ReservationStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Operação requer status {expected}, atual {Status}.");
    }
}
