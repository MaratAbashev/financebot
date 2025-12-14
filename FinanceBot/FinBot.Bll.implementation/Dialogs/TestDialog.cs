using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs;

public class TestDialog(ITelegramBotClient botClient)//: IDialogDefinition
{
    public string DialogName => "TestDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        { 0, new TextStep<string>("textStep1", "Question1", _ => 1, _ => -1, true) },
        { 1, new TextStep<int>("textStep2", "Question2", _ => 2,  _ => 0) },
        { 2, new TextStep<string>("textStep3", "Question3", _ => -1, _ => 1) }
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