using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Blocks;

public record CreateBlockCommand(
    BlockTargetType TargetType,
    Guid TargetId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason) : IRequest<Guid>;

public class CreateBlockValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
    }
}

public class CreateBlockHandler(IAppDbContext db, ICurrentUserService current)
    : IRequestHandler<CreateBlockCommand, Guid>
{
    public async Task<Guid> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        if (current.UserId is null) throw new UnauthorizedException("Not authenticated.");

        var pavilionId = await ResolvePavilionIdAsync(db, request.TargetType, request.TargetId, ct);
        if (current.Role != UserRole.Admin)
        {
            var manages = await db.PavilionManagers.AnyAsync(m =>
                m.PavilionId == pavilionId && m.UserId == current.UserId.Value, ct);
            if (!manages) throw new ForbiddenException("User does not manage the affected pavilion.");
        }

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var block = new Block(request.TargetType, request.TargetId, period, request.Reason, current.UserId.Value);
        db.Blocks.Add(block);
        await db.SaveChangesAsync(ct);
        return block.Id;
    }

    private static async Task<Guid> ResolvePavilionIdAsync(IAppDbContext db, BlockTargetType type, Guid targetId, CancellationToken ct)
    {
        switch (type)
        {
            case BlockTargetType.Pavilion:
                var pav = await db.Pavilions.AnyAsync(p => p.Id == targetId, ct);
                if (!pav) throw new NotFoundException("Pavilion not found.");
                return targetId;
            case BlockTargetType.Room:
                var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == targetId, ct)
                    ?? throw new NotFoundException("Room not found.");
                var floor = await db.Floors.FirstAsync(f => f.Id == room.FloorId, ct);
                return floor.PavilionId;
            case BlockTargetType.Desk:
                var desk = await db.Desks.FirstOrDefaultAsync(d => d.Id == targetId, ct)
                    ?? throw new NotFoundException("Desk not found.");
                var deskRoom = await db.Rooms.FirstAsync(r => r.Id == desk.RoomId, ct);
                var deskFloor = await db.Floors.FirstAsync(f => f.Id == deskRoom.FloorId, ct);
                return deskFloor.PavilionId;
            default:
                throw new BusinessRuleException("Unknown target type.");
        }
    }
}
