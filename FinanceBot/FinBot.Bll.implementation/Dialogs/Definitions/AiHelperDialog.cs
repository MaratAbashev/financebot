using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Cache;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class AiHelperDialog : IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IAsyncProducerProvider _provider;
    private readonly ITelegramBotClient _botClient;
    private readonly ICacheStorage _cacheStorage;
    private readonly ILogger<AiHelperDialog> _logger;

    public AiHelperDialog(IMediator mediator, IAsyncProducerProvider provider, ITelegramBotClient botClient,
        ICacheStorage cacheStorage, ILogger<AiHelperDialog> logger)
    {
        _mediator = mediator;
        _provider = provider;
        _botClient = botClient;
        _cacheStorage = cacheStorage;
        _logger = logger;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder(-1);

        builder.AddChoiceStep<string>(
                "mock",
                "mock",
                _ => [])
            .WithDataLoader(_ => Task.FromResult(Result<IEnumerable<string>>.Failure("mock")))
            .OnPromptFailed(async (_, chatId, update, _) =>
            {
                await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId));
            })
            .Commit();
        builder.AddChoiceStep<int>(
                "chooseAiAction",
                "Получите анализ или совет от ИИ помощника",
                _ =>
                [
                    ("Анализ", 0),
                    ("Совет", 1)
                ])
            .Commit();
        builder.AddChoiceStep(
                "chooseReportDuration",
                "За какой период отчёт вы желаете получить?",
                _ => (List<(string, int)>)
                [
                    ("День", (int)TimeInterval.Day),
                    ("Неделя", (int)TimeInterval.Week),
                    ("Месяц", (int)TimeInterval.Month),
                ])
            .Commit();

        return builder.Build();
    }

    public string DialogName => nameof(AiHelperDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("AiHelperDialog completed for ChatId={ChatId}, extracting dialog context", chatId);

        if (!dialogContext.TryGetData("chooseAiAction", out int action)
            || !dialogContext.TryGetData("chooseGroup", out string groupStr)
            || !dialogContext.TryGetData<int>("chooseReportDuration", out var timeInterval)
            || !Guid.TryParse(groupStr, out var groupId))
        {
            _logger.LogWarning(
                "Failed to extract dialog context for ChatId={ChatId}. " +
                "chooseAiAction={HasAction}, chooseGroup={HasGroup}, chooseReportDuration={HasDuration}, groupIdValid={GroupIdValid}",
                chatId,
                dialogContext.TryGetData("chooseAiAction", out int _),
                dialogContext.TryGetData("chooseGroup", out string group),
                dialogContext.TryGetData<int>("chooseReportDuration", out _),
                Guid.TryParse(group, out _));
            return;
        }

        var analyseMode = (AnalyseMode)action;
        var timeIntervalEnum = (TimeInterval)timeInterval;
        var cacheKey = string.Join('_', groupId, chatId, analyseMode, timeIntervalEnum, DateTime.UtcNow.Date);

        _logger.LogInformation(
            "Processing AI request for ChatId={ChatId}, GroupId={GroupId}, AnalyseMode={AnalyseMode}, TimeInterval={TimeInterval}",
            chatId, groupId, analyseMode, timeIntervalEnum);

        var existingAnalysis = await _cacheStorage.GetAsync<string>(cacheKey);
        if (existingAnalysis != null)
        {
            _logger.LogInformation(
                "Cache hit for ChatId={ChatId}, GroupId={GroupId}, AnalyseMode={AnalyseMode}, TimeInterval={TimeInterval}, CacheKey={CacheKey}",
                chatId, groupId, analyseMode, timeIntervalEnum, cacheKey);

            await _botClient.SendMessage(chatId, existingAnalysis.EscapeMarkdownV2(),
                cancellationToken: cancellationToken);
            return;
        }

        _logger.LogInformation(
            "Cache miss for CacheKey={CacheKey}. Producing AI analyse message to Kafka for ChatId={ChatId}, GroupId={GroupId}",
            cacheKey, chatId, groupId);

        var producer = _provider.GetProducer<AiAnalyseMessage, LlmTopic>();
        var message = new AiAnalyseMessage
        {
            UserId = chatId,
            GroupId = groupId,
            AnalyseMode = analyseMode,
            TimeInterval = timeIntervalEnum
        };

        await producer.ProduceAsync(message, cancellationToken);

        _logger.LogDebug("AiAnalyseMessage produced to Kafka for ChatId={ChatId}, GroupId={GroupId}", chatId, groupId);

        await _botClient.SendMessage(chatId, "Ваш анализ отправлен на обработку", cancellationToken: cancellationToken);
    }
}