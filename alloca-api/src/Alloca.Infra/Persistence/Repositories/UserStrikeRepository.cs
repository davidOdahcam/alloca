using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class UserStrikeRepository(AllocaDbContext db) : Repository<UserStrike>(db), IUserStrikeRepository
{
    public Task<int> CountActiveAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
        => Set.CountAsync(s => s.UserId == userId && s.ExpiresAt > nowUtc, ct);
}
