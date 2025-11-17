using System.Data.Common;
using System.Linq.Expressions;
using FinBot.Bll.Interfaces;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FinBot.Dal;

public class GenericRepository<T, TKey, TContext>(TContext context) : IGenericRepository<T, TKey, TContext>
    where T : class, IBusinessEntity<TKey>, new()
    where TKey : struct
    where TContext : DbContext
{
    private readonly DbSet<T> _dbSet = context.Set<T>();
    private readonly TContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public virtual T First(Expression<Func<T, bool>> predicate) => _dbSet.First(predicate);

    public virtual T? FirstOrDefault(Expression<Func<T, bool>>? predicate = null) => predicate == null ? _dbSet.FirstOrDefault() : _dbSet.FirstOrDefault(predicate);

    public virtual IQueryable<T> GetAll(bool asTrack = false) => !asTrack ? _dbSet.AsNoTracking() : _dbSet;

    public IAsyncEnumerable<T> AsAsyncEnumerable() => _dbSet.AsAsyncEnumerable();

    public virtual IQueryable<T> FindBy(Expression<Func<T, bool>> predicate) => _dbSet.Where(predicate);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => await _dbSet.AnyAsync(predicate);

    public virtual async Task<T> FindAsync(params TKey[] keys) => (await _dbSet.FindAsync(keys))!;

    public virtual async ValueTask<EntityEntry<T>> AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public virtual void AddRange(IEnumerable<T> entities) => _dbSet.AddRange(entities);

    public virtual void Delete(T entity) => _dbSet.Remove(entity);

    public virtual async Task DeleteByIdAsync(TKey id)
    {
        var async = await _dbSet.FindAsync(id);
        if (async == null)
            return;
        _dbSet.Remove(async);
    }

    public async Task DeleteRangeAsync(IEnumerable<TKey> ids) =>
        _dbSet.RemoveRange(await _dbSet
            .Where(dS => ids.Contains(dS.Id)).ToListAsync());

    public void DeleteRangeAsync(IEnumerable<T> entity) => _dbSet.RemoveRange(entity);

    public virtual void Update(T entity) => _context.Entry(entity).State = EntityState.Modified;

    public virtual void SaveChanges() => _context.SaveChanges();

    public virtual Task SaveChangesAsync() => _context.SaveChangesAsync(CancellationToken.None);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null) =>
        predicate == null
            ? await _dbSet.FirstOrDefaultAsync()
            : await _dbSet.FirstOrDefaultAsync(predicate);

    public IOrderedQueryable<T> OrderBy<TK>(Expression<Func<T, TK>> predicate) => _dbSet.OrderBy(predicate);

    public IQueryable<IGrouping<TK, T>> GroupBy<TK>(Expression<Func<T, TK>> predicate) => _dbSet.GroupBy(predicate);

    public virtual void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    public virtual void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public virtual async Task<T?> GetByIdAsync(TKey key) => await _dbSet.FindAsync(key);

    public async Task<T> Upsert(T entity) => await _dbSet.FindAsync(entity.Id) ?? (await _dbSet.AddAsync(entity)).Entity;

    public IQueryable<T> Execute(string query, DbParameter sqlParam) => _dbSet.FromSqlRaw(query, sqlParam);

    public IQueryable<T> ExecuteString(string query) => _dbSet.FromSqlRaw(query);
}