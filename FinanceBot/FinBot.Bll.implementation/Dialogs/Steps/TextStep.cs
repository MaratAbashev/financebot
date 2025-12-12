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
    Func<DialogContext, int> prevStepId,
    bool isFirstStep = false): IStep where T: IConvertible 
{
    private readonly Func<T, Result>? _validate;
    public bool IsFirstStep { get; init; } = isFirstStep;
    public string Key { get; init; } = key;
    public Func<DialogContext, int> NextStepId { get; init; } = nextStepId;
    public Func<DialogContext, int> PrevStepId { get; init; } = prevStepId;

    public TextStep(string key, string question, Func<DialogContext, int> nextStepId, Func<DialogContext, int> prevStepId, Func<T, Result> validate, bool isFirstStep = false): this(key, question, nextStepId, prevStepId, isFirstStep)
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
                : ReplyKeyboardBuilder.CreateBackButton(dialogContext.DialogName),
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
        catch (Exception ex) when (ex is FormatException or InvalidCastException)
        {
            return Task.FromResult(Result.Failure("Вы ввели данные некорректно, попробуйте еще раз", ErrorType.Validation));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(ex.Message));
        }
    }
}