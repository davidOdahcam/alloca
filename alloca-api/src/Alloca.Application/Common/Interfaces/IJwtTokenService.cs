using Alloca.Domain.Entities;

namespace Alloca.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) Generate(User user);
}
