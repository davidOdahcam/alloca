using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class Desk : Entity
{
    public Guid RoomId { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsReservable { get; private set; }

    private Desk() { }

    public Desk(Guid roomId, string externalId, string name, bool isReservable)
    {
        if (string.IsNullOrWhiteSpace(externalId)) throw new DomainException("O identificador externo da mesa é obrigatório.");
        if (!externalId.StartsWith("DESK-")) throw new DomainException("O identificador externo da mesa deve iniciar com DESK-.");
        RoomId = roomId;
        ExternalId = externalId.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsReservable = isReservable;
    }

    public void SetReservable(bool isReservable)
    {
        IsReservable = isReservable;
        Touch();
    }
}
