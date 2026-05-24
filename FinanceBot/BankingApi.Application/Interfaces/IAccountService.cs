using BankingApi.Application.Dto;

namespace BankingApi.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountDto>> GetUserAccountsAsync(Guid userId, CancellationToken ct = default);
    Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountDto dto, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(Guid accountId, Guid userId, CancellationToken ct = default);
}