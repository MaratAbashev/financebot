using System.Text.RegularExpressions;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs.Steps;

public abstract class DataStep(
    string key, // ваш баланс {{balance}} рублей
    Func<DialogContext, int> nextStepId,
    Func<DialogContext, int> prevStepId,
    Func<DialogContext, Task<Result<IEnumerable<string>>>>? dataLoader = null, // подгружает данные в контекст
    // возвращает список ключей для замены (например как balance выше)
    // если вернул ErrorResult то сам промт вернет
    // ErrorResult и будет выполняться OnPromptFailed
    bool isFirstStep = false,
    Func<Result, long, Update, DialogContext, Task>? onPromptFailed = null)
    : IStep
{
    public bool IsFirstStep { get; init; } = isFirstStep;
    public string Key { get; init; } = key;
    public Func<DialogContext, int> NextStepId { get; init; } = nextStepId;
    public Func<DialogContext, int> PrevStepId { get; init; } = prevStepId;
    public Func<Result, long, Update, DialogContext, Task>? OnPromptFailed { get; init; } = onPromptFailed;
    protected readonly Func<DialogContext, Task<Result<IEnumerable<string>>>> DataLoader = dataLoader ?? (_ => Task.FromResult(Result<IEnumerable<string>>.Success([])));

    protected Result<string> FormatPrompt(string promptTemplate, DialogContext dialogContext, IEnumerable<string> keysToReplace)
    {
        var prompt = promptTemplate;
        foreach (var key in keysToReplace)
        {
            if (dialogContext.DialogStorage == null 
                || !dialogContext.DialogStorage.TryGetValue(key, out var value))
                return Result<string>.Failure($"Cant get value for {key} to replace key");
            prompt =  prompt.Replace($"{{{{{key}}}}}", value.ToString());
        }

        prompt = Regex.Replace(prompt, @"[^a-zA-Zа-яА-Я0-9\s]", "");
        return Result<string>.Success(prompt);
    }
    
    public abstract Task<Result> PromptAsync(ITelegramBotClient client, long chatId, DialogContext dialogContext,
        CancellationToken cancellationToken);

    public abstract Task<Result> HandleAsync(ITelegramBotClient client, Update update, DialogContext dialogContext,
        CancellationToken cancellationToken);
}