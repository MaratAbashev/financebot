using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.BankService.Repositories;

public class BankConnectionRepository(PDbContext context) : IBankConnectionRepository
{
    public async Task<BankConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.BankConnections.FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task<List<BankConnection>> GetAllActiveAsync(CancellationToken ct = default)
        => await context.BankConnections.Where(c => c.IsActive).ToListAsync(ct);

    public async Task AddAsync(BankConnection connection, CancellationToken ct = default)
    {
        context.BankConnections.Add(connection);
        await context.SaveChangesAsync(ct);
    }
    
    public async Task UpdateAsync(BankConnection connection, CancellationToken ct = default)
    {
        context.BankConnections.Update(connection);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        await context.BankConnections
            .Where(c => c.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.RefreshToken, refreshToken)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);
    }
}