using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs;

public class MenuDialog(IMediator mediator): IDialogDefinition
{
    public string DialogName => "MenuDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new ChoiceStep<string>(
                "menuStep",
                "Главное меню",
                _ => -1, 
                _ => -1,
                _ => [
                    ("Создать копилку", "CreateGroupDialog"),
                    ("Внести пополнение", "AddExpenseDialog"),
                    ("Добавить пользователя", "AddUserDialog"),
                    ("Управление копилками", "ManageGroupsDialog")
                ],
                isFirstStep: true)
        }
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext,
        Update update, CancellationToken cancellationToken)
    {
        if (dialogContext.DialogStorage == null 
            || !dialogContext.DialogStorage.TryGetValue("menuStep", out var menuStep)
            || menuStep is not string menuStepStr)
        {
            return;
        }
        await mediator.Send(new StartDialogRequest(update, menuStepStr, chatId), cancellationToken);
    }
}