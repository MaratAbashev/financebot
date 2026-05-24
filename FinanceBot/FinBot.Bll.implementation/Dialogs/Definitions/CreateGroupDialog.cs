using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class CreateGroupDialog : IDialogDefinition
{
    private readonly IGroupService _groupService;
    private readonly IUserService _userService;
    private readonly ITelegramBotClient _botClient;
    private readonly IMediator _mediator;

    public CreateGroupDialog(IGroupService groupService,
        IUserService userService,
        ITelegramBotClient botClient,
        IMediator mediator)
    {
        _groupService = groupService;
        _userService = userService;
        _botClient = botClient;
        _mediator = mediator;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder();
        builder.AddTextStep<string>(
                "groupName",
                "Введите название группы")
            .WithValidation(value => value.Length is > 0 and <= 20
                ? Result.Success()
                : Result.Failure("Длина названия должна быть от 1 до 20 символов"))
            .AsFirstStep()
            .Commit();
        builder.AddTextStep<decimal>(
                "replenishment",
                @"Введите пополнение группы (если не целое то через точку)")
            .WithValidation(value => value > 0m
                ? Result.Success()
                : Result.Failure("Введите число больше нуля"))
            .Commit();
        var hasTargetStep = builder.AddChoiceStep<bool>(
                "hasTarget",
                "Вы хотите откладывать оставшиеся деньги в копилку?",
                _ =>
                [
                    ("Да", true),
                    ("Нет", false)
                ])
            .Commit();
        var targetNameStep = builder.AddTextStep<string>(
                "targetName",
                "На что хотите накопить?")
            .WithValidation(value => value.Length is > 0 and <= 20
                ? Result.Success()
                : Result.Failure("Длина названия должна быть от 1 до 20 символов"))
            .Commit();
        var targetAmountStep = builder.AddTextStep<decimal>(
                "targetAmount",
                @"Сколько вам нужно накопить? (если не целое то через точку)")
            .WithValidation(value => value > 0m
                ? Result.Success()
                : Result.Failure("Введите число больше нуля"))
            .Commit();
        var debtStrategyStep = builder
            .AddChoiceStep(
                "debtStrategy",
                "Что делать с долгами если не рассчитали расходы?",
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Прощаем", (int)DebtStrategy.Nullify),
                        ("Берем с пополнения следующего месяца", (int)DebtStrategy.FromNextMonth)
                    ];
                    if (ctx.TryGetData("hasTarget", out bool hasTarget)
                        && hasTarget)
                        buttons.Add(("Берем с копилки", (int)DebtStrategy.FromSaving));
                    return buttons;
                })
            .Commit();
        hasTargetStep.GoTo(ctx =>
        {
            if (ctx.TryGetData("hasTarget", out bool hasTarget)
                && hasTarget)
                return targetNameStep.Id;
            return debtStrategyStep.Id;
        });
        debtStrategyStep.Back(ctx =>
        {
            if (ctx.TryGetData("hasTarget", out bool hasTarget)
                && hasTarget)
                return targetAmountStep.Id;
            return hasTargetStep.Id;
        });
        builder.AddChoiceStep(
                "daySavingStrategy",
                "Что делать с остатком денег в конце дня?",
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Делим на остаток периода", (int)SavingStrategy.Spread),
                        ("Оставляем на следующий месяц", (int)SavingStrategy.SaveForNextPeriod)
                    ];
                    if (ctx.TryGetData("hasTarget", out bool hasTarget)
                        && hasTarget)
                        buttons.Add(("Кладем в копилку", (int)SavingStrategy.Save));
                    return buttons;
                })
            .Commit();
        builder.AddChoiceStep(
                "periodSavingStrategy",
                "Что делать с остатком денег в конце месяца?",
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Оставляем на следующий месяц", (int)SavingStrategy.SaveForNextPeriod)
                    ];
                    if (ctx.DialogStorage != null
                        && ctx.DialogStorage.TryGetValue("hasTarget", out var hasTarget)
                        && hasTarget is true)
                        buttons.Add(("Кладем в копилку", (int)SavingStrategy.Save));
                    return buttons;
                })
            .Commit();
        return builder.Build();
    }

    public string DialogName => "CreateGroupDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext,
        Update update, CancellationToken cancellationToken)
    {
        var getUserResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!getUserResult.IsSuccess)
            return;
        if (!dialogContext.TryGetData<string>("groupName", out var groupName)
            || !dialogContext.TryGetData<decimal>("replenishment", out var replenishment)
            || !dialogContext.TryGetData<int>("debtStrategy", out var debtStrategy)
            || !dialogContext.TryGetData<int>("daySavingStrategy", out var daySavingStrategy)
            || !dialogContext.TryGetData<int>("periodSavingStrategy", out var periodSavingStrategy)
           )
            return;
        dialogContext.TryGetData<string?>("targetName", out var targetName);
        dialogContext.TryGetData<decimal>("targetAmount", out var targetAmount);
        var createGroupResult = await _groupService.CreateGroupAsync(
            groupName,
            getUserResult.Data.TelegramId,
            replenishment,
            (SavingStrategy)periodSavingStrategy,
            (SavingStrategy)daySavingStrategy,
            (DebtStrategy)debtStrategy,
            targetName,
            targetAmount);

        await _botClient.SendMessage(
            chatId,
            createGroupResult.IsSuccess
                ? "Группа успешно создана"
                : "Не удалось создать группу, попробуйте позже",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}