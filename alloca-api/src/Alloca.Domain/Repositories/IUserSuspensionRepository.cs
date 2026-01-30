using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IUserSuspensionRepository : IRepository<UserSuspension>
{
    Task<bool> IsCurrentlySuspendedAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
}
