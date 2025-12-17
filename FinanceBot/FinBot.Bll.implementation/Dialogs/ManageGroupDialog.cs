using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace FinBot.Bll.Implementation.Dialogs;

public class ManageGroupDialog(
    ITelegramBotClient botClient,
    ILogger<ManageGroupDialog> logger) : IDialogDefinition
{
    public string DialogName => nameof(ManageGroupDialog);

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, new TextStep<string>(
                "main",
                @"Вот ваша копилка на **{goal}**",
                _ => 1,
                _ => -1,
                true)
        },
        {
            1, new TextStep<string>(
                "endDayLogic",
                "Что будем делать с деньгами в конце **дня**?\n0 Копим\n1 Размазываем на месяц",
                _ => 2,
                _ => 0,
                false)
        },
        {
            2, new TextStep<string>(
                "endMonthLogic",
                "Что будем делать с деньгами в конце **месяца**?\n0 Копим\n1 Переносим на следующий месяц",
                context =>
                {
                    if (int.Parse((string)context.DialogStorage!["endMonthLogic"]) == 0)
                        return 3;
                    
                    return -1;
                },
                _ => 0,
                false)
        },
        {
            3, new TextStep<string>(
                "goal",
                "На что копим?",
                _ => 4,
                _ => 2,
                false)
        },
        {
            4, new TextStep<string>(
                "moneyGoal",
                @"Сколько нам на это копить целое \- число рублей?",
                _ => -1,
                _ => 3,
                false)
        }
    };

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        var replenishment = int.Parse((string)dialogContext.DialogStorage!["replenishment"]);
        var endDayLogic = int.Parse((string)dialogContext.DialogStorage!["endDayLogic"]);
        var endMonthLogic = int.Parse((string)dialogContext.DialogStorage!["endMonthLogic"]);
        var goal = endMonthLogic == 0 ? (string)dialogContext.DialogStorage!["goal"] : null;
        var moneyGoal = endDayLogic == 0 ? int.Parse((string)dialogContext.DialogStorage!["moneyGoal"]) : 0;
    }
}