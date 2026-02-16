using Alloca.Domain.Common;
using Alloca.Domain.Enums;
using Alloca.Domain.ValueObjects;

namespace Alloca.Domain.Entities;

public class Block : Entity
{
    public BlockTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public TimeRange Period { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid CreatedByUserId { get; private set; }

    private Block() { }

    public Block(BlockTargetType targetType, Guid targetId, TimeRange period, string reason, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Informe o motivo do bloqueio.");
        TargetType = targetType;
        TargetId = targetId;
        Period = period;
        Reason = reason.Trim();
        CreatedByUserId = createdByUserId;
    }
}
