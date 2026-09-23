using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Users;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Services;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class UserAppService(
    IUserService userService,
    IPasswordHasher hasher,
    ICurrentUserService currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ResetUserPasswordRequest> resetValidator) : IUserAppService
{
    public async Task<IReadOnlyList<UserListItem>> ListAsync(string? search, string? role, bool? isActive, CancellationToken ct = default)
    {
        UserRole? parsedRole = null;
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var r))
            parsedRole = r;

        var list = await userService.ListAsync(search, parsedRole, isActive, ct);
        return list.Select(ToListItem).ToList();
    }

    public async Task<UserDetail> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userService.GetAsync(id, ct);
        return ToDetail(user);
    }

    public async Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ConflictException(ErrorCodes.UserInvalidRole, "Role inválido.");

        var user = await userService.CreateAsync(request.Email, request.FullName, hasher.Hash(request.Password), role, ct);
        return ToDetail(user);
    }

    public async Task<UserDetail> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ConflictException(ErrorCodes.UserInvalidRole, "Role inválido.");

        var user = await userService.UpdateAsync(id, request.FullName, role, currentUser.UserId, ct);
        return ToDetail(user);
    }

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
        => userService.DeactivateAsync(id, currentUser.UserId, ct);

    public Task ActivateAsync(Guid id, CancellationToken ct = default)
        => userService.ActivateAsync(id, ct);

    public async Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken ct = default)
    {
        await resetValidator.ValidateAndThrowAsync(request, ct);
        await userService.ResetPasswordAsync(id, hasher.Hash(request.NewPassword), ct);
    }

    private static UserListItem ToListItem(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);

    private static UserDetail ToDetail(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
}
