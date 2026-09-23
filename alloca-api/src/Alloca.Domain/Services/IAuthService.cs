using Alloca.Domain.Entities;
using Alloca.Domain.Enums;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pelas regras de autenticação e registro de usuários.
/// </summary>
public interface IAuthService
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User> RegisterAsync(string email, string fullName, string passwordHash, UserRole role, CancellationToken ct = default);
}
