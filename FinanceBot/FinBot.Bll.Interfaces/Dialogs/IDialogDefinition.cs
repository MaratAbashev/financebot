using FinBot.Domain.Models;

namespace FinBot.Bll.Interfaces.Dialogs;

public interface IDialogDefinition
{
    public string DialogName { get; }
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public Task OnCompletedAsync(long chatId, DialogContext dialogContext, CancellationToken cancellationToken);
}