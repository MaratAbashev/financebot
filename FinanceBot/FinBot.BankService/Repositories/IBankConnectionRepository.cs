using FinBot.BankService.Models;
using FinBot.Domain.Models;

namespace FinBot.BankService.Repositories;

public interface IBankConnectionRepository
{
    Task<BankConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<BankConnection>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(BankConnection connection, CancellationToken ct = default);
    Task UpdateAsync(BankConnection connection, CancellationToken ct = default);
    Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}