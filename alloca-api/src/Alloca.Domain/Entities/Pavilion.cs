using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class Pavilion : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int MinAdvanceMinutes { get; private set; } = 5;
    public int MaxAdvanceDays { get; private set; } = 30;
    public int SlotMinutes { get; private set; } = 30;

    private readonly List<Floor> _floors = new();
    public IReadOnlyCollection<Floor> Floors => _floors.AsReadOnly();

    private readonly List<OperatingHours> _operatingHours = new();
    public IReadOnlyCollection<OperatingHours> OperatingHours => _operatingHours.AsReadOnly();

    private readonly List<PavilionManager> _managers = new();
    public IReadOnlyCollection<PavilionManager> Managers => _managers.AsReadOnly();

    private Pavilion() { }

    public Pavilion(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Pavilion code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Pavilion name is required.");
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }

    public void UpdatePolicy(int minAdvanceMinutes, int maxAdvanceDays, int slotMinutes)
    {
        if (minAdvanceMinutes < 0) throw new DomainException("MinAdvance cannot be negative.");
        if (maxAdvanceDays <= 0) throw new DomainException("MaxAdvance must be positive.");
        if (slotMinutes <= 0 || slotMinutes > 240) throw new DomainException("Slot must be 1..240 min.");
        MinAdvanceMinutes = minAdvanceMinutes;
        MaxAdvanceDays = maxAdvanceDays;
        SlotMinutes = slotMinutes;
        Touch();
    }

    public Floor AddFloor(string code, string name, int level, string? svgKey = null)
    {
        var floor = new Floor(Id, code, name, level, svgKey);
        _floors.Add(floor);
        return floor;
    }

    public OperatingHours SetOperatingHours(DayOfWeek day, TimeOnly opensAt, TimeOnly closesAt)
    {
        var existing = _operatingHours.FirstOrDefault(o => o.DayOfWeek == day);
        if (existing is not null)
        {
            existing.Update(opensAt, closesAt);
            return existing;
        }
        var oh = new OperatingHours(Id, day, opensAt, closesAt);
        _operatingHours.Add(oh);
        return oh;
    }
}
