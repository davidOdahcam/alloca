using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Blocks;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using Alloca.Domain.ValueObjects;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class BlockService(
    IBlockRepository blocks,
    IPavilionRepository pavilions,
    IRoomRepository rooms,
    IDeskRepository desks,
    IFloorRepository floors,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IValidator<CreateBlockRequest> validator) : IBlockService
{
    public async Task<CreateBlockResponse> CreateAsync(CreateBlockRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        if (currentUser.UserId is null) throw new UnauthorizedException("Not authenticated.");

        var pavilionId = await ResolvePavilionIdAsync(request.TargetType, request.TargetId, ct);
        if (currentUser.Role != UserRole.Admin)
        {
            var manages = await pavilions.ManagesAsync(pavilionId, currentUser.UserId.Value, ct);
            if (!manages) throw new ForbiddenException("User does not manage the affected pavilion.");
        }

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        var period = new TimeRange(startUtc, endUtc);

        var block = new Block(request.TargetType, request.TargetId, period, request.Reason, currentUser.UserId.Value);
        blocks.Add(block);
        await uow.SaveChangesAsync(ct);
        return new CreateBlockResponse(block.Id);
    }

    private async Task<Guid> ResolvePavilionIdAsync(BlockTargetType type, Guid targetId, CancellationToken ct)
    {
        switch (type)
        {
            case BlockTargetType.Pavilion:
                if (!await pavilions.AnyAsync(p => p.Id == targetId, ct))
                    throw new NotFoundException("Pavilion not found.");
                return targetId;
            case BlockTargetType.Room:
                var room = await rooms.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException("Room not found.");
                var floor = await floors.GetByIdAsync(room.FloorId, ct)
                    ?? throw new NotFoundException("Floor not found.");
                return floor.PavilionId;
            case BlockTargetType.Desk:
                var desk = await desks.GetByIdAsync(targetId, ct)
                    ?? throw new NotFoundException("Desk not found.");
                var deskRoom = await rooms.GetByIdAsync(desk.RoomId, ct)
                    ?? throw new NotFoundException("Room not found.");
                var deskFloor = await floors.GetByIdAsync(deskRoom.FloorId, ct)
                    ?? throw new NotFoundException("Floor not found.");
                return deskFloor.PavilionId;
            default:
                throw new BusinessRuleException("Unknown target type.");
        }
    }
}
