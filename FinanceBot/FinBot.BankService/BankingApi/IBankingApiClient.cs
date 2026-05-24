using FinBot.Domain.Utils;

namespace FinBot.BankService.BankingApi;

public interface IBankingApiClient
{
    Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<List<TransactionDto>>> GetTransactionsAsync(Guid accountId, string accessToken, CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetAccountsAsync(string accessToken, CancellationToken ct = default);
}