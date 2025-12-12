using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.implementation.Dialogs.Steps;

public class TextStep<T>(
    string key,
    string question, 
    Func<DialogContext, int> nextStepId,
    bool isFirstStep = false): IStep 
{
    private readonly Func<T, Result>? _validate;
    public bool IsFirstStep { get; init; } = isFirstStep;
    public string Key { get; init; } = key;
    public Func<DialogContext, int> NextStepId { get; init; } = nextStepId;
    public TextStep(string key, string question, Func<DialogContext, int> nextStepId, Func<T, Result> validate, bool isFirstStep = false): this(key, question, nextStepId, isFirstStep)
    {
        _validate = validate;
    }
    public async Task PromptAsync(ITelegramBotClient client, long chatId, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        await client.SendMessage(chatId,
            question,
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: IsFirstStep 
                ? null
                : ReplyKeyboardBuilder.CreateBackButton(dialogContext.DialogName, dialogContext.CurrentStep),
            cancellationToken: cancellationToken);
    }

    public Task<Result> HandleAsync(ITelegramBotClient client, Update update, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: not null })
            return Task.FromResult(
                Result.Failure($"Update type is {update.Type}, expected type is {UpdateType.Message}"));
        try
        {
            var message = update.Message;
            var valueToAdd = (T)Convert.ChangeType(message.Text, typeof(T));
            if (_validate != null)
            {
                var validationResult = _validate(valueToAdd);
                if (!validationResult.IsSuccess)
                    return Task.FromResult(Result.Failure(validationResult.ErrorMessage!, ErrorType.Validation));
            }
            if (dialogContext.DialogStorage != null) 
                dialogContext.DialogStorage[Key] = valueToAdd;
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(ex.Message));
        }
        
        return Task.FromResult(Result.Failure($"Update type is {update.Type}, expected type is {UpdateType.Message}"));
    }
}