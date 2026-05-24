using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.BankService.Repositories;

public class BankTransactionRepository(PDbContext context) : IBankTransactionRepository
{
    public async Task<List<BankTransaction>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.BankTransactions
            .Where(t => t.UserId == userId && t.Status == BankTransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default)
    {
        context.BankTransactions.AddRange(transactions);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid externalId, CancellationToken ct = default)
        => await context.BankTransactions.AnyAsync(t => t.ExternalId == externalId, ct);
}