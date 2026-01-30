using System.Linq.Expressions;
using Alloca.Domain.Common;
using Alloca.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence.Repositories;

public class Repository<T>(AllocaDbContext db) : IRepository<T> where T : Entity
{
    protected readonly AllocaDbContext Db = db;
    protected DbSet<T> Set => Db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Set.AnyAsync(predicate, ct);

    public IQueryable<T> Query() => Set.AsQueryable();

    public void Add(T entity) => Set.Add(entity);
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
}
