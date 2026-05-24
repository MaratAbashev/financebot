using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IAiService
{
    Task<Result<string>> GetAnalysisAsync(long userTgId, Guid groupId, CancellationToken cancellationToken = default);

    Task<Result<string>> GetAdviceAsync(long userTgId, Guid groupId, CancellationToken cancellationToken = default);
}