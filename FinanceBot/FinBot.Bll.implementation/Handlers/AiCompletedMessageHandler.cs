using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Cache;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Messages;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Handlers;

public class AiCompletedMessageHandler(ITelegramBotClient botClient,
    ICacheStorage cacheStorage, ILogger<AiCompletedMessageHandler> logger): IMessageHandler<AiAnalyseCompletedMessage>
{
    public async Task HandleAsync(AiAnalyseCompletedMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Received AiAnalyseCompletedMessage for UserId={UserId}, AnalysisCacheId={AnalysisCacheId}",
            message.UserId, message.AnalysisCacheId);

        var analyse = await cacheStorage.GetAsync<string>(message.AnalysisCacheId);
        if (analyse == null)
        {
            logger.LogError(
                "Analysis not found in cache for UserId={UserId}, AnalysisCacheId={AnalysisCacheId}",
                message.UserId, message.AnalysisCacheId);

            await botClient.SendMessage(
                message.UserId,
                "Не удалось найти анализ ваших трат",
                parseMode: ParseMode.None,
                cancellationToken: cancellationToken);
        }

        logger.LogDebug(
            "Analysis retrieved from cache for UserId={UserId}, AnalysisCacheId={AnalysisCacheId}, Length={Length}",
            message.UserId, message.AnalysisCacheId, analyse.Length);

        await botClient.SendMessage(
            message.UserId,
            analyse.EscapeMarkdownV2(),
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Analysis successfully delivered to UserId={UserId}, AnalysisCacheId={AnalysisCacheId}",
            message.UserId, message.AnalysisCacheId);
    }
}