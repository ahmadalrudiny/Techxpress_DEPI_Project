using System.Linq.Expressions;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();   // للفلاتر والـ Pagination
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}