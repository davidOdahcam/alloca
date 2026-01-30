using Alloca.Domain.Entities;

namespace Alloca.Domain.Repositories;

public interface IUserStrikeRepository : IRepository<UserStrike>
{
    Task<int> CountActiveAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
}
