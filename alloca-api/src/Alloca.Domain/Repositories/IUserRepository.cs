using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}
