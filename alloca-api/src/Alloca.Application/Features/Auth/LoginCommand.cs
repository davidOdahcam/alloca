using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public record LoginResponse(string Token, DateTime ExpiresAtUtc, Guid UserId, string FullName, string Email, string Role);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive) throw new UnauthorizedException("User is inactive.");
        if (!hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var (token, exp) = jwt.Generate(user);
        return new LoginResponse(token, exp, user.Id, user.FullName, user.Email, user.Role.ToString());
    }
}
