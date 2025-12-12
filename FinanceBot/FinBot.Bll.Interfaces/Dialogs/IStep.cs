using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinBot.Bll.Interfaces.Dialogs;

public interface IStep
{
    public bool IsFirstStep { get; init; }
    public string Key { get; init; }
    public Func<DialogContext, int> NextStepId { get; init; }
    public Func<DialogContext, int> PrevStepId { get; init; }
    public Task PromptAsync(ITelegramBotClient client, long chatId, DialogContext dialogContext, CancellationToken cancellationToken);
    public Task<Result> HandleAsync(ITelegramBotClient client, Update update, DialogContext dialogContext, CancellationToken cancellationToken);
}