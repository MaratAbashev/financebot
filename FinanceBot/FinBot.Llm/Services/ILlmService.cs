using FinBot.Domain.Models.Enums;

namespace FinBot.Llm.Services;

public interface ILlmService
{
    Task<string> GetAnalysisAsync(long userTgId, Guid groupId, TimeInterval timeInterval, CancellationToken cancellationToken = default);

    Task<string> GetAdviceAsync(long userTgId, Guid groupId, TimeInterval timeInterval, CancellationToken cancellationToken = default);
}