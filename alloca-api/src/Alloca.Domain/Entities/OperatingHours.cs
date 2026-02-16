using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class OperatingHours : Entity
{
    public Guid PavilionId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpensAt { get; private set; }
    public TimeOnly ClosesAt { get; private set; }
    public bool IsClosed { get; private set; }

    private OperatingHours() { }

    public OperatingHours(Guid pavilionId, DayOfWeek day, TimeOnly opensAt, TimeOnly closesAt)
    {
        PavilionId = pavilionId;
        DayOfWeek = day;
        Update(opensAt, closesAt);
    }

    public void Update(TimeOnly opensAt, TimeOnly closesAt)
    {
        if (closesAt <= opensAt) throw new DomainException("O horário de fechamento deve ser posterior à abertura.");
        OpensAt = opensAt;
        ClosesAt = closesAt;
        IsClosed = false;
        Touch();
    }

    public void MarkClosed()
    {
        IsClosed = true;
        Touch();
    }
}
