using Alloca.Domain.Entities;
using Alloca.Domain.Enums;

namespace Alloca.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(string? search, UserRole? role, bool? isActive, CancellationToken ct = default);
}
