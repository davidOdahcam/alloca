using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class Desk : Entity
{
    public Guid RoomId { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    private Desk() { }

    public Desk(Guid roomId, string externalId, string name)
    {
        if (string.IsNullOrWhiteSpace(externalId)) throw new DomainException("Desk ExternalId required.");
        if (!externalId.StartsWith("DESK-")) throw new DomainException("Desk ExternalId must start with DESK-.");
        RoomId = roomId;
        ExternalId = externalId.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}
