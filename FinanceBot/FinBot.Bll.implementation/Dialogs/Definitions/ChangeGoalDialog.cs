using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class ChangeGoalDialog: IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IGroupService _groupService;
    private readonly ITelegramBotClient _botClient;

    public ChangeGoalDialog(IMediator mediator,
        IGroupService groupService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _groupService = groupService;
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
        var savingFlagStep = builder.AddChoiceStep(
            "savingFlag",
            "Копим?",
            _ =>
            {
                List<(string, bool)> buttons =
                [
                    ("Копим", true),
                    ("Нет", false)
                ];

                return buttons;
            })
            .Commit();

        var savingTargetStep = builder.AddTextStep<string>(
                "targetName",
                @"На что хотите накопить?")
            .WithValidation(value => value.Length is > 0 and <= 20
                ? Result.Success()
                : Result.Failure("Длина названия должна быть от 1 до 20 символов"))
            .Commit();
        savingTargetStep.Back(savingFlagStep);
        
        var savingAmountStep = builder.AddTextStep<decimal>(
                "targetAmount",
                @"Сколько вам нужно накопить? (если не целое то через точку)")
            .WithValidation(value => value > 0m
                ? Result.Success()
                : Result.Failure("Введите число больше нуля"))
            .Commit();
        savingAmountStep.Back(savingTargetStep);

        savingFlagStep.GoTo(ctx =>
            (bool)ctx.DialogStorage!["savingFlag"]
                ? savingTargetStep.Id
                : -10);
        savingTargetStep.GoTo(_ => savingAmountStep.Id);
        savingAmountStep.GoTo(_ => -10);
        return builder.Build();
    }

    public string DialogName => nameof(ChangeGoalDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData<string>("chooseGroup", out var groupId)
            || !Guid.TryParse(groupId, out var groupIdGuid)
            || !dialogContext.TryGetData<bool>("savingFlag", out var savingFlag))
            return;
        
        var toggleSavingResult = await _groupService.ToggleSavingAsync(groupIdGuid, savingFlag);
        if (!toggleSavingResult.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                "Не удалось изменить настройки копилки, попробуйте еще раз",
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            return;
        }

        if (!savingFlag)
            await _botClient.SendMessage(
                chatId,
                "Копилка отключена",
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);

        if (!dialogContext.TryGetData<string>("targetName", out var targetName)
            || !dialogContext.TryGetData<decimal>("targetAmount", out var targetAmount))
            return;

        var updateGroupResult = await _groupService.ChangeGoalAsync(groupIdGuid, targetName, targetAmount);

        await _botClient.SendMessage(
            chatId,
            updateGroupResult.IsSuccess
                ? "Копилка изменена"
                : "Не удалось изменить настройки копилки, попробуйте еще раз",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}