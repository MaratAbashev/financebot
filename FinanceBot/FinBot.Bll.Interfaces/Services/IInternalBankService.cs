using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IInternalBankService
{
    public Task<Result<bool>> IsBankConnectedAsync(long userTgId, CancellationToken ct);
}