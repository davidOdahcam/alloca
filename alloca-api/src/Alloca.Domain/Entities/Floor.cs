using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class Floor : Entity
{
    public Guid PavilionId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int Level { get; private set; }
    public string? SvgKey { get; private set; }

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private Floor() { }

    public Floor(Guid pavilionId, string code, string name, int level, string? svgKey)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Floor code is required.");
        PavilionId = pavilionId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Level = level;
        SvgKey = svgKey;
    }

    public Room AddRoom(string externalId, string name, bool isReservable)
    {
        var room = new Room(Id, externalId, name, isReservable);
        _rooms.Add(room);
        return room;
    }
}
