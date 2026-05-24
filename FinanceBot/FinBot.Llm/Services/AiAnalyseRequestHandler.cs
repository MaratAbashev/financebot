using FinBot.Cache;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;
using FinBot.Observability.Metrics;

namespace FinBot.Llm.Services;

public class AiAnalyseRequestHandler(
    ILlmService llmService,
    ICacheStorage cacheStorage,
    BusinessMetrics businessMetrics,
    ILogger<AiAnalyseRequestHandler> logger) : ITransactionMessageHandler<AiAnalyseMessage>
{
    public async Task HandleAsync(AiAnalyseMessage message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Analysis started for UserId={UserId}, GroupId={GroupId}, AnalyseMode={AnalyseMode}, TimeInterval={TimeInterval}",
            message.UserId, message.GroupId, message.AnalyseMode, message.TimeInterval);
 
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
 
            var analysis = message.AnalyseMode switch
            {
                AnalyseMode.Advice => await llmService.GetAdviceAsync(
                    message.UserId, message.GroupId, message.TimeInterval, cancellationToken),
                AnalyseMode.Analyse => await llmService.GetAnalysisAsync(
                    message.UserId, message.GroupId, message.TimeInterval, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(message.AnalyseMode), message.AnalyseMode, "Analyse mode is invalid value")
            };
 
            sw.Stop();
            logger.LogInformation(
                "LLM response received for UserId={UserId}, GroupId={GroupId}, AnalyseMode={AnalyseMode}, " +
                "ElapsedMs={ElapsedMs}, ResponseLength={ResponseLength}",
                message.UserId, message.GroupId, message.AnalyseMode, sw.ElapsedMilliseconds, analysis.Length);
 
            var analysisCacheId = string.Join('_',
                message.GroupId, message.UserId, message.AnalyseMode, message.TimeInterval, DateTime.UtcNow.Date);
 
            await cacheStorage.SetAsync(analysisCacheId, analysis, TimeSpan.FromMinutes(15));
 
            logger.LogDebug(
                "Analysis cached with CacheId={AnalysisCacheId}, TTL=15min, UserId={UserId}, GroupId={GroupId}",
                analysisCacheId, message.UserId, message.GroupId);
 
            var producer = context.GetProducer<AiAnalyseCompletedMessage, ApiLlmTopic>();
            var completedMessage = new AiAnalyseCompletedMessage
            {
                AnalysisCacheId = analysisCacheId,
                UserId = message.UserId
            };
 
            producer.Produce(completedMessage);
 
            logger.LogInformation(
                "AiAnalyseCompletedMessage produced for UserId={UserId}, CacheId={AnalysisCacheId}",
                message.UserId, analysisCacheId);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogError(ex,
                "Invalid AnalyseMode={AnalyseMode} for UserId={UserId}, GroupId={GroupId}",
                message.AnalyseMode, message.UserId, message.GroupId);

            businessMetrics.LlmOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", "error"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "An error occurred during analysis for UserId={UserId}, GroupId={GroupId}, AnalyseMode={AnalyseMode}",
                message.UserId, message.GroupId, message.AnalyseMode);

            businessMetrics.LlmOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", "error"));
        }
    }
}