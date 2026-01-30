using Alloca.Domain.Repositories;

namespace Alloca.Infra.Persistence.Repositories;

public class UnitOfWork(AllocaDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
