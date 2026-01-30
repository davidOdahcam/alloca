using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class UserRepository(AllocaDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.AnyAsync(u => u.Email == normalized, ct);
    }
}
