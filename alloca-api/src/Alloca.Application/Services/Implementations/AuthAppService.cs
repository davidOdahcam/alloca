using Alloca.Application.Common.Interfaces;
using Alloca.Application.DTOs.Auth;
using Alloca.Domain.Common.Exceptions;
using Alloca.Domain.Enums;
using Alloca.Domain.Services;
using FluentValidation;

namespace Alloca.Application.Services.Implementations;

public class AuthAppService(
    IAuthService authService,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterRequest> registerValidator) : IAuthAppService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await authService.FindByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException(ErrorCodes.UserInvalidCredentials, "E-mail ou senha incorretos.");

        if (!user.IsActive) throw new UnauthorizedException(ErrorCodes.UserInactive, "Sua conta está inativa. Procure um administrador.");
        if (!hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(ErrorCodes.UserInvalidCredentials, "E-mail ou senha incorretos.");

        var (token, exp) = jwt.Generate(user);
        return new LoginResponse(token, exp, user.Id, user.FullName, user.Email, user.Role.ToString());
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var user = await authService.RegisterAsync(request.Email, request.FullName, hasher.Hash(request.Password), UserRole.Member, ct);
        return new RegisterResponse(user.Id);
    }
}
