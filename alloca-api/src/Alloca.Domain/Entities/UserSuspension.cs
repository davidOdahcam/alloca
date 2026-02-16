using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class UserSuspension : Entity
{
    public Guid UserId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid? IssuedByUserId { get; private set; }

    private UserSuspension() { }

    public UserSuspension(Guid userId, DateTime startsAt, DateTime endsAt, string reason, Guid? issuedByUserId)
    {
        if (endsAt <= startsAt) throw new DomainException("O fim da suspensão deve ser posterior ao início.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Informe o motivo da suspensão.");
        UserId = userId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Reason = reason.Trim();
        IssuedByUserId = issuedByUserId;
    }

    public bool IsActive(DateTime nowUtc) => nowUtc >= StartsAt && nowUtc < EndsAt;
}
