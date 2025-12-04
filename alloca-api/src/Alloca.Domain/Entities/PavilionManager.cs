using Alloca.Domain.Common;

namespace Alloca.Domain.Entities;

public class PavilionManager : Entity
{
    public Guid PavilionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

    private PavilionManager() { }

    public PavilionManager(Guid pavilionId, Guid userId)
    {
        PavilionId = pavilionId;
        UserId = userId;
    }
}
