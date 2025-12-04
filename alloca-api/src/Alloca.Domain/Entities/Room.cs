using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class Room : Entity
{
    public Guid FloorId { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsReservable { get; private set; }
    public int? Capacity { get; private set; }

    private readonly List<Desk> _desks = new();
    public IReadOnlyCollection<Desk> Desks => _desks.AsReadOnly();

    private Room() { }

    public Room(Guid floorId, string externalId, string name, bool isReservable)
    {
        if (string.IsNullOrWhiteSpace(externalId)) throw new DomainException("Room ExternalId required.");
        if (!externalId.StartsWith("ROOM-")) throw new DomainException("Room ExternalId must start with ROOM-.");
        FloorId = floorId;
        ExternalId = externalId.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsReservable = isReservable;
    }

    public void SetCapacity(int? capacity)
    {
        if (capacity is < 0) throw new DomainException("Capacity cannot be negative.");
        Capacity = capacity;
        Touch();
    }

    public Desk AddDesk(string externalId, string name)
    {
        var desk = new Desk(Id, externalId, name);
        _desks.Add(desk);
        return desk;
    }
}
