using Alloca.Domain.Common;

namespace Alloca.Domain.ValueObjects;

public class TimeRange : IEquatable<TimeRange>
{
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }

    private TimeRange() { }

    public TimeRange(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("O intervalo de tempo precisa estar em UTC.");
        if (endUtc <= startUtc)
            throw new DomainException("A data/hora final deve ser posterior à inicial.");

        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public TimeSpan Duration => EndUtc - StartUtc;

    public bool Overlaps(TimeRange other) =>
        StartUtc < other.EndUtc && other.StartUtc < EndUtc;

    public bool Contains(DateTime utc) => utc >= StartUtc && utc < EndUtc;

    public bool Equals(TimeRange? other) =>
        other is not null && StartUtc == other.StartUtc && EndUtc == other.EndUtc;

    public override bool Equals(object? obj) => Equals(obj as TimeRange);
    public override int GetHashCode() => HashCode.Combine(StartUtc, EndUtc);
}

