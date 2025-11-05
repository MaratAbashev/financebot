using System.Data.Common;
using System.Linq.Expressions;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FinBot.Bll.Interfaces;

public interface IGenericRepository<T, in TKey, TContext>
    where T : class, IBusinessEntity<TKey>, new()
    where TKey : struct
    where TContext : DbContext
{
    void SaveChanges();

    Task SaveChangesAsync();

    T First(Expression<Func<T, bool>> predicate);

    T? FirstOrDefault(Expression<Func<T, bool>>? predicate = null);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null);

    IQueryable<T> GetAll(bool asTrack = false);

    IAsyncEnumerable<T> AsAsyncEnumerable();

    IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    Task<T> FindAsync(params TKey[] keys);

    Task<T?> GetByIdAsync(TKey key);

    ValueTask<EntityEntry<T>> AddAsync(T entity);

    void AddRange(IEnumerable<T> entities);

    void Delete(T entity);

    Task DeleteByIdAsync(TKey id);

    Task DeleteRangeAsync(IEnumerable<TKey> entity);

    void Update(T entity);

    Task<T> Upsert(T entity);

    IOrderedQueryable<T> OrderBy<TK>(Expression<Func<T, TK>> predicate);

    IQueryable<IGrouping<TK, T>> GroupBy<TK>(Expression<Func<T, TK>> predicate);

    void RemoveRange(IEnumerable<T> entities);

    void UpdateRange(IEnumerable<T> entities);

    IQueryable<T> Execute(string query, DbParameter sqlParam);

    IQueryable<T> ExecuteString(string query);

    void DeleteRangeAsync(IEnumerable<T> entities);
}