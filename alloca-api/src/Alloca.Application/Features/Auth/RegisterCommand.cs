using Alloca.Application.Common.Exceptions;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Features.Auth;

public record RegisterCommand(string Email, string FullName, string Password) : IRequest<Guid>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterCommandHandler(IAppDbContext db, IPasswordHasher hasher)
    : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email already in use.");

        var user = new User(email, request.FullName, hasher.Hash(request.Password), UserRole.Member);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }
}
