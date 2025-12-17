using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs;

public class TestDialog(ITelegramBotClient botClient, IMediator mediator): IDialogDefinition
{
    public string DialogName => "TestDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new TextStep<string>(
                "textStep1", 
                "Время: {{time}}", 
                _ => 1, 
                _ => -1,
                ctx=>
                {
                    ctx.DialogStorage!["time"] = DateTime.Now.ToShortTimeString();
                    return Task.FromResult(Result<IEnumerable<string>>.Success(["time"]));
                },
                true)
        },
        {
            1, 
            new ChoiceStep<string>(
                "choiceStep1",
                "Выбери вариант ответа",
                _ => 2,
                _ => 0,
                _ => [
                    ("Вариант 1", "val1"), 
                    ("Вариант 2", "val2")
                    ]
                )
        },
        {
            2, 
            new ChoiceStep<string>(
                "choiceStep2",
                "Предыдущий вариант ответа: {{choiceStep1}}\nВыбери вариант ответа",
                _ => 3,
                _ => 1,
                _ => [
                    ("Вариант 1", "val1"), 
                    ("Вариант 2", "val2")
                ],
                _ => Task.FromResult(Result<IEnumerable<string>>.Success(["choiceStep1"]))
            )
            
        },
        {
            3, new TextStep<string>(
            "textStep2", 
            "Шаг 4", 
            _ => -1, 
            _ => 2,
            _ => 
                Task.FromResult(Result<IEnumerable<string>>.Failure("test error")),
            onPromptFailed: async (_, userId, updateType, _) =>
            {
                await mediator.Send(new StartDialogRequest(updateType, "TestDialog2", userId));
            }
            )
        }
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId,
            $"{dialogContext.DialogStorage!["textStep1"]} {dialogContext.DialogStorage["textStep2"]}",
            parseMode: ParseMode.MarkdownV2
            , cancellationToken: cancellationToken);
    }
}