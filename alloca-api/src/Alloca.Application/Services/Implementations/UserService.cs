using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Users;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Services.Implementations;

public class UserService(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher hasher,
    ICurrentUserService currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ResetUserPasswordRequest> resetValidator) : IUserService
{
    public async Task<IReadOnlyList<UserListItem>> ListAsync(string? search, string? role, bool? isActive, CancellationToken ct = default)
    {
        var query = users.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var list = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItem(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(ct);

        return list;
    }

    public async Task<UserDetail> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        return ToDetail(user);
    }

    public async Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        if (await users.EmailExistsAsync(request.Email, ct))
            throw new ConflictException(ErrorCodes.UserEmailInUse, "E-mail já está em uso.");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ConflictException(ErrorCodes.UserInvalidRole, "Role inválido.");

        var user = new User(request.Email, request.FullName, hasher.Hash(request.Password), role);
        users.Add(user);
        await uow.SaveChangesAsync(ct);

        return ToDetail(user);
    }

    public async Task<UserDetail> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var user = await users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ConflictException(ErrorCodes.UserInvalidRole, "Role inválido.");

        // Impede que o admin logado se rebaixe (perderia acesso à própria gestão).
        if (currentUser.UserId == id && user.Role == UserRole.Admin && role != UserRole.Admin)
            throw new ForbiddenException(ErrorCodes.UserCannotDemoteSelf, "Você não pode rebaixar a sua própria conta de administrador.");

        user.UpdateFullName(request.FullName);
        if (user.Role != role) user.PromoteTo(role);

        users.Update(user);
        await uow.SaveChangesAsync(ct);
        return ToDetail(user);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.UserId == id)
            throw new ForbiddenException(ErrorCodes.UserCannotDeactivateSelf, "Você não pode desativar a sua própria conta.");

        var user = await users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        if (!user.IsActive) return;
        user.Deactivate();
        users.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        if (user.IsActive) return;
        user.Activate();
        users.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken ct = default)
    {
        await resetValidator.ValidateAndThrowAsync(request, ct);
        var user = await users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        user.UpdatePasswordHash(hasher.Hash(request.NewPassword));
        users.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    private static UserDetail ToDetail(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
}
