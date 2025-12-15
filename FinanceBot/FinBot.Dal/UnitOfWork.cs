using FinBot.Bll.Interfaces;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FinBot.Dal;

public class UnitOfWork<TContext>(TContext context, ILogger<UnitOfWork<TContext>> logger) : IUnitOfWork<TContext>, IDisposable, IAsyncDisposable where TContext : DbContext
{
    public TContext CommonContext { get; }
    
    public IDbContextTransaction? CurrentTransaction => CommonContext.Database.CurrentTransaction;

    public async Task<Result> SaveChangesAsync()
    {
        try
        {
            await CommonContext.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during save changes: {errorMessage}", ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    public IDbContextTransaction BeginDbTransaction()
    {
        return CommonContext.Database.BeginTransaction();
    }

    public void Dispose()
    {
        CastAndDispose(context);
        CastAndDispose(CommonContext);

        return;

        static void CastAndDispose(IAsyncDisposable resource)
        {
            if (resource is IDisposable resourceDisposable)
                resourceDisposable.Dispose();
            else
                _ = resource.DisposeAsync().AsTask();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await CommonContext.DisposeAsync();
    }
}