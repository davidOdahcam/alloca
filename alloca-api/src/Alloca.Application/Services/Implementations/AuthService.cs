using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Auth;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Alloca.Domain.Repositories;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class AuthService(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterRequest> registerValidator) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await users.FindByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive) throw new UnauthorizedException("User is inactive.");
        if (!hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var (token, exp) = jwt.Generate(user);
        return new LoginResponse(token, exp, user.Id, user.FullName, user.Email, user.Role.ToString());
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        if (await users.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("Email already in use.");

        var user = new User(request.Email, request.FullName, hasher.Hash(request.Password), UserRole.Member);
        users.Add(user);
        await uow.SaveChangesAsync(ct);
        return new RegisterResponse(user.Id);
    }
}
