using FinBot.BankService.Models;
using FinBot.Domain.Utils;

namespace FinBot.BankService.Services;

public interface IBankAuthService
{
    Task<Result<string>> GetAuthUrlAsync(Guid userId, CancellationToken ct = default);
    Task HandleCallbackAsync(OAuthCallbackPayload payload, CancellationToken ct = default);
    Task UnlinkBankAsync(Guid userId, CancellationToken ct = default);
}