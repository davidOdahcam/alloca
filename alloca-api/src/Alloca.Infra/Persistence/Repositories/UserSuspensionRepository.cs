using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class UserSuspensionRepository(AllocaDbContext db) : Repository<UserSuspension>(db), IUserSuspensionRepository
{
    public Task<bool> IsCurrentlySuspendedAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
        => Set.AnyAsync(s => s.UserId == userId && s.StartsAt <= nowUtc && nowUtc < s.EndsAt, ct);
}
