using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Options;
using FinBot.Domain.Reports;
using FinBot.Domain.Utils;
using FinBot.MinIOS3;
using MediatR;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class ExcelMetricsDialog: IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IReportService _reportService; 
    private readonly IMinioStorage _minioStorage;
    private readonly StorageOptions _storageOptions;
    private readonly ITelegramBotClient _botClient;

    public ExcelMetricsDialog(
        IMediator mediator,
        IUserService userService,
        IReportService reportService,
        IMinioStorage minioStorage,
        IOptions<StorageOptions> storageOptions,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _userService = userService;
        _reportService = reportService;
        _minioStorage = minioStorage;
        _storageOptions = storageOptions.Value;
        _botClient = botClient;
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
                "getGroupStatistic",
                "Вы собираетесь получить статистику за последний месяц",
                _ =>
                [
                    ("Мою", (int)ReportType.ForUser),
                    ("Группы", (int)ReportType.ForGroup)
                ])
            .Commit();
        builder.AddChoiceStep(
                "chooseReportType",
                "Какой отчёт вы желаете получить?",
                _ => (List<(string, int)>)
                [
                    ("Гистограмма", (int)ExcelType.ColumnChart),
                    ("Линейный график", (int)ExcelType.LineChart)
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
    
    public string DialogName => nameof(ExcelMetricsDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        var userResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!userResult.IsSuccess)
            return;
        if (!dialogContext.TryGetData<string>("chooseGroup", out var groupId)
            || !dialogContext.TryGetData<int>("getGroupStatistic", out var reportType)
            || !dialogContext.TryGetData<int>("chooseReportType", out var excelType)
            || !dialogContext.TryGetData<int>("chooseReportDuration", out var timeInterval)
            || !Guid.TryParse(groupId, out var groupIdGuid))
            return;

        var period = PeriodCalculator.ForPrevious((TimeInterval)timeInterval, DateTime.UtcNow);
        var fileName = ReportObjectName.Build(chatId, groupIdGuid, (ReportType)reportType, (ExcelType)excelType, period.Key);
        var bucketName = excelType switch
        {
            0 => _storageOptions.ExcelTablesBucket,
            1 => _storageOptions.BarChartsBucket,
            2 => _storageOptions.LineChartsBucket,
            _ => throw new ArgumentOutOfRangeException()
        };

        var storageExistResult = await _minioStorage.ExistsAsync(bucketName, fileName, cancellationToken);
        if (storageExistResult is { IsSuccess: true, Data: true })
        {
            var storageGetResult = await _minioStorage.GetAsync(bucketName, fileName, cancellationToken);
            if (storageGetResult.IsSuccess)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Статистика за данный интервал уже посчитана",
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                await _botClient.SendDocument(
                    chatId,
                    InputFile.FromStream(new MemoryStream(storageGetResult.Data)),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
                return;
            }
        }

        var excelResult = await _reportService.GenerateReportAsync(chatId, groupIdGuid, (ReportType)reportType,
            (ExcelType)excelType,
            (TimeInterval)timeInterval, cancellationToken);
        await _botClient.SendMessage(
            chatId,
            excelResult.IsSuccess
                ? "Генерация файла запущена, отправим как будет готово"
                : "Что-то пошло не так, попробуйте позже",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }
}