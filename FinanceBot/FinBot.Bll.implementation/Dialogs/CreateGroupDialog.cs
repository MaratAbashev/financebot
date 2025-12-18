using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs;

public class CreateGroupDialog(
    IGroupService groupService, 
    IUserService userService,
    ITelegramBotClient botClient,
    IMediator mediator): IDialogDefinition
{
    public string DialogName => "CreateGroupDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new TextStep<string>(
                "groupName", 
                "Введите название группы", 
                _ => 1, 
                _ => -1,
                isFirstStep: true)
        },
        {
            1, 
            new TextStep<decimal>(
                "replenishment", 
                @"Введите пополнение группы \(если не целое то через точку\)", 
                _ => 2, 
                _ => 0,
                validate: 
                value => value > 0m
                    ? Result.Success() 
                    : Result.Failure("Введите число больше нуля"))
        },
        {
            2, 
            new ChoiceStep<bool>(
                "hasTarget",
                "Вы хотите откладывать оставшиеся деньги в копилку?",
                ctx =>
                {
                    if (ctx.DialogStorage == null
                        || !ctx.DialogStorage.TryGetValue("hasTarget", out var hasTarget)
                        || hasTarget is not bool hasTargetBool
                        || !hasTargetBool)
                        return 5;
                    return 3;
                }, 
                _ => 1,
                _ => [
                    ("Да", true),
                    ("Нет", false)
                ])
        },
        {
            3,
            new TextStep<string>(
                "targetName", 
                "На что хотите накопить?", 
                _ => 4, 
                _ => 2)
        },
        {
            4, 
            new TextStep<decimal>(
                "targetAmount", 
                @"Сколько вам нужно накопить? \(если не целое то через точку\)", 
                _ => 5, 
                _ => 3,
                validate: 
                value => value > 0m
                    ? Result.Success() 
                    : Result.Failure("Введите число больше нуля"))
        },
        {
            5, 
            new ChoiceStep<int>(
                "debtStrategy",
                "Что делать с долгами если не рассчитали расходы?",
                _ => 6,
                ctx =>
                {
                    if (ctx.DialogStorage == null
                        || !ctx.DialogStorage.TryGetValue("hasTarget", out var hasTarget)
                        || hasTarget is not bool hasTargetBool
                        || !hasTargetBool)
                        return 2;
                    return 4;
                },
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Прощаем", (int)DebtStrategy.Nullify),
                        ("Берем с пополнения следующего месяца", (int)DebtStrategy.FromNextMonth)
                    ];
                    if (ctx.DialogStorage != null
                        && ctx.DialogStorage.TryGetValue("hasTarget", out var hasTarget)
                        && hasTarget is true)
                        buttons.Add(("Берем с копилки", (int)DebtStrategy.FromSaving));
                    return buttons;
                })
        },
        {
            6, 
            new ChoiceStep<int>(
                "daySavingStrategy",
                "Что делать с остатком денег в конце дня?",
                _ => 7,
                _ => 5,
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Делим на остаток периода", (int)SavingStrategy.Spread),
                        ("Оставляем на следующий месяц", (int)SavingStrategy.SaveForNextPeriod)
                    ];
                    if (ctx.DialogStorage != null
                        && ctx.DialogStorage.TryGetValue("hasTarget", out var hasTarget)
                        && hasTarget is true)
                        buttons.Add(("Кладем в копилку", (int)SavingStrategy.Save));
                    return buttons;
                })
        },
        {
            7, 
            new ChoiceStep<int>(
                "periodSavingStrategy",
                "Что делать с остатком денег в конце месяца?",
                _ => -1,
                _ => 6,
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
        }
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext,
        Update update, CancellationToken cancellationToken)
    {
        var getUserResult = await userService.GetUserByTgIdAsync(chatId);
        if (!getUserResult.IsSuccess
            || getUserResult.Data == null)
            return;
        if (!TryGetData<string>(dialogContext, "groupName", out var groupName)
            || !TryGetData<decimal>(dialogContext, "replenishment", out var replenishment)
            || !TryGetData<int>(dialogContext, "debtStrategy", out var debtStrategy)
            || !TryGetData<int>(dialogContext, "daySavingStrategy", out var daySavingStrategy)
            || !TryGetData<int>(dialogContext, "periodSavingStrategy", out var periodSavingStrategy))
            return;
        
        dialogContext.DialogStorage!.TryGetValue("targetName", out var targetNameBoxed);
        var targetName = targetNameBoxed?.ToString();
        decimal? targetAmount = null;
        try
        {
            if (dialogContext.DialogStorage!.TryGetValue("targetAmount", out var targetAmountBoxed))
                targetAmount = Convert.ToDecimal(targetAmountBoxed);
        }
        catch (Exception)
        {
            targetAmount = null;
        }

        var createGroupResult = await groupService.CreateGroupAsync(
            groupName,
            getUserResult.Data,
            replenishment,
            (SavingStrategy)periodSavingStrategy,
            (SavingStrategy)daySavingStrategy,
            (DebtStrategy)debtStrategy,
            targetName,
            targetAmount);
        if (!createGroupResult.IsSuccess)
        {
            return;
        }
        await botClient.SendMessage(
            chatId,
            "Группа успешно создана",
            parseMode: ParseMode.MarkdownV2, 
            cancellationToken: cancellationToken);
        await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
    }

    private bool TryGetData<T>(DialogContext dialogContext, string key, out T data) where T : IConvertible
    {
        data = default!;  // всегда инициализируем
    
        if (dialogContext.DialogStorage == null
            || !dialogContext.DialogStorage.TryGetValue(key, out var boxedData))
        {
            return false;  // нет данных
        }

        try
        {
            // pattern matching + Convert.ChangeType
            if (boxedData is T directData)
            {
                data = directData;
                return true;
            }
        
            data = (T)Convert.ChangeType(boxedData, typeof(T));
            return true;
        }
        catch
        {
            return false;  // конверсия не удалась
        }
    }
}