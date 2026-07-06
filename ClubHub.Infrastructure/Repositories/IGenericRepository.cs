using System.Linq.Expressions;

namespace ClubHub.API.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    IQueryable<T> Query();
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
}
