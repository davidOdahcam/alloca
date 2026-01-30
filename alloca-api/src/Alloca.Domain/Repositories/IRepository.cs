using System.Linq.Expressions;
using Alloca.Domain.Common;

namespace Alloca.Domain.Repositories;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    IQueryable<T> Query();
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
