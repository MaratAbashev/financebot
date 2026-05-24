using FinBot.BankService.Models;
using FinBot.Domain.Models;

namespace FinBot.BankService.Repositories;

public interface IBankTransactionRepository
{
    Task<List<BankTransaction>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid externalId, CancellationToken ct = default);
}