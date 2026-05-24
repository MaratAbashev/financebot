using BankingApi.Application.Dto;

namespace BankingApi.Application.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid accountId, Guid userId, CancellationToken ct = default);

    Task<TransactionDto> CreateTransactionAsync(Guid accountId, Guid userId, CreateTransactionDto dto,
        CancellationToken ct = default);

    Task<SummaryDto> GetSummaryAsync(Guid accountId, Guid userId, CancellationToken ct = default);
}