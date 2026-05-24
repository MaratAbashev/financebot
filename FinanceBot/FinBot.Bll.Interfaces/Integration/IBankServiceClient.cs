using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IBankServiceClient
{
    Task<Result<string>> GetAuthUrlAsync(Guid userId, CancellationToken ct);
    Task<Result<int>> SynchronizeTransactionsAsync(Guid userId, CancellationToken ct);
    Task<Result<string>> UnlinkBankAsync(Guid userId, CancellationToken ct);
}