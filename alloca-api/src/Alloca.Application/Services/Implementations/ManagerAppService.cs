using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Manager;
using Alloca.Application.DTOs.Reservations;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Enums;
using Alloca.Domain.ReadModels;
using Alloca.Domain.Services;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class ManagerAppService(
    IManagerService managerService,
    ICurrentUserService currentUser,
    IValidator<ReasonRequest> reasonValidator) : IManagerAppService
{
    public async Task<IReadOnlyList<ReservationResponse>> ListPendingAsync(Guid? pavilionId, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var views = await managerService.ListPendingAsync(userId, IsAdmin, pavilionId, ct);
        return views.Select(ToResponse).ToList();
    }

    public Task ApproveAsync(Guid reservationId, CancellationToken ct = default)
        => managerService.ApproveAsync(reservationId, RequireUserId(), IsAdmin, ct);

    public async Task RejectAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default)
    {
        await reasonValidator.ValidateAndThrowAsync(request, ct);
        await managerService.RejectAsync(reservationId, RequireUserId(), IsAdmin, request.Reason, ct);
    }

    public async Task RevokeAsync(Guid reservationId, ReasonRequest request, CancellationToken ct = default)
    {
        await reasonValidator.ValidateAndThrowAsync(request, ct);
        await managerService.RevokeAsync(reservationId, RequireUserId(), IsAdmin, request.Reason, ct);
    }

    private bool IsAdmin => currentUser.Role == UserRole.Admin;

    private Guid RequireUserId()
    {
        if (currentUser.UserId is null) throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Você precisa estar autenticado.");
        return currentUser.UserId.Value;
    }

    private static ReservationResponse ToResponse(ReservationView v) =>
        new(v.Id, v.ResourceType, v.RoomId, v.DeskId, v.ResourceExternalId, v.ResourceName,
            v.PavilionId, v.PavilionName, v.StartUtc, v.EndUtc, v.Status, v.Notes,
            v.DecisionReason, v.CheckedInAt, v.CreatedAt);
}
