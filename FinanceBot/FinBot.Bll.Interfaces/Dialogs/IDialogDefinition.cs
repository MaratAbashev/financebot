using FinBot.Domain.Models;
using Telegram.Bot.Types;

namespace FinBot.Bll.Interfaces.Dialogs;

public interface IDialogDefinition
{
    public string DialogName { get; }
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public Task OnCompletedAsync(long chatId, DialogContext dialogContext,
        Update update, CancellationToken cancellationToken);
}