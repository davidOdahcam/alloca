using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;

namespace Alloca.Domain.Services.Implementations;

public class UserService(IUserRepository userRepository, IUnitOfWork uow) : IUserService
{
    public Task<IReadOnlyList<User>> ListAsync(string? search, UserRole? role, bool? isActive, CancellationToken ct = default)
        => userRepository.ListAsync(search, role, isActive, ct);

    public async Task<User> GetAsync(Guid id, CancellationToken ct = default)
        => await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");

    public async Task<User> CreateAsync(string email, string fullName, string passwordHash, UserRole role, CancellationToken ct = default)
    {
        if (await userRepository.EmailExistsAsync(email, ct))
            throw new ConflictException(ErrorCodes.UserEmailInUse, "E-mail já está em uso.");

        var user = new User(email, fullName, passwordHash, role);
        userRepository.Add(user);
        await uow.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(Guid id, string fullName, UserRole role, Guid? actorUserId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");

        // Impede que o admin logado se rebaixe (perderia acesso à própria gestão).
        if (actorUserId == id && user.Role == UserRole.Admin && role != UserRole.Admin)
            throw new ForbiddenException(ErrorCodes.UserCannotDemoteSelf, "Você não pode rebaixar a sua própria conta de administrador.");

        user.UpdateFullName(fullName);
        if (user.Role != role) user.PromoteTo(role);

        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
        return user;
    }

    public async Task DeactivateAsync(Guid id, Guid? actorUserId, CancellationToken ct = default)
    {
        if (actorUserId == id)
            throw new ForbiddenException(ErrorCodes.UserCannotDeactivateSelf, "Você não pode desativar a sua própria conta.");

        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        if (!user.IsActive) return;
        user.Deactivate();
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        if (user.IsActive) return;
        user.Activate();
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, string newPasswordHash, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Usuário não encontrado.");
        user.UpdatePasswordHash(newPasswordHash);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
