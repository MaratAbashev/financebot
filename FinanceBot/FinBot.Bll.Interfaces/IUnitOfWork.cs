using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinBot.Bll.Interfaces;

public interface IUnitOfWork<TContext> : IDisposable where TContext : DbContext
{
    TContext CommonContext { get; }

    IDbContextTransaction? CurrentTransaction { get; }

    Task<Result> SaveChangesAsync();

    IDbContextTransaction BeginDbTransaction();
}