using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs;

public class TestDialog2(ITelegramBotClient botClient): IDialogDefinition
{
    public string DialogName => "TestDialog2";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new TextStep<string>(
                "textStep1", 
                "Новый диалог", 
                _ => 1, 
                _ => -1,
                isFirstStep: true)
        },
        {
            1, 
            new ChoiceStep<string>(
                "choiceStep1",
                "Выбери вариант ответа",
                _ => -1,
                _ => 0,
                _ => [
                    ("Вариант 1", "val1"), 
                    ("Вариант 2", "val2")
                    ]
                )
        }
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId,
            $"{dialogContext.DialogStorage!["textStep1"]} {dialogContext.DialogStorage["choiceStep1"]}",
            parseMode: ParseMode.MarkdownV2
            , cancellationToken: cancellationToken);
    }
}