using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;

namespace Alloca.Domain.Services.Implementations;

public class AuthService(IUserRepository userRepository, IUnitOfWork uow) : IAuthService
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => userRepository.FindByEmailAsync(email, ct);

    public async Task<User> RegisterAsync(string email, string fullName, string passwordHash, UserRole role, CancellationToken ct = default)
    {
        if (await userRepository.EmailExistsAsync(email, ct))
            throw new ConflictException(ErrorCodes.UserEmailInUse, "Este e-mail já está em uso.");

        var user = new User(email, fullName, passwordHash, role);
        userRepository.Add(user);
        await uow.SaveChangesAsync(ct);
        return user;
    }
}
