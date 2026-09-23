using Alloca.Domain.Entities;
using Alloca.Domain.Enums;

namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pelas regras de gestão de usuários.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<User>> ListAsync(string? search, UserRole? role, bool? isActive, CancellationToken ct = default);
    Task<User> GetAsync(Guid id, CancellationToken ct = default);
    Task<User> CreateAsync(string email, string fullName, string passwordHash, UserRole role, CancellationToken ct = default);
    Task<User> UpdateAsync(Guid id, string fullName, UserRole role, Guid? actorUserId, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, Guid? actorUserId, CancellationToken ct = default);
    Task ActivateAsync(Guid id, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, string newPasswordHash, CancellationToken ct = default);
}
