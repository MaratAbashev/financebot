using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Steps;

public class TextStep<T>(
    string key,
    string promptTemplate,
    Func<DialogContext, int> nextStepId,
    Func<DialogContext, int> prevStepId,
    Func<DialogContext, Task<Result<IEnumerable<string>>>>? dataLoader = null,
    bool isFirstStep = false,
    Func<T, Result>? validate = null,
    Func<Result, long, Update, DialogContext, Task>? onPromptFailed = null
    ): DataStep(key, nextStepId, prevStepId, dataLoader, isFirstStep, onPromptFailed) where T: IConvertible 
{
    public override async Task<Result> PromptAsync(ITelegramBotClient client, long chatId, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        var loadDataResult = await DataLoader(dialogContext);
        if (!loadDataResult.IsSuccess)
            return Result.Failure(loadDataResult.ErrorMessage!);
        var promptResult = FormatPrompt(promptTemplate, dialogContext, loadDataResult.Data);
        if (!promptResult.IsSuccess)
            return Result.Failure(promptResult.ErrorMessage!);
        await client.SendMessage(chatId, 
            promptResult.Data, 
            replyMarkup: IsFirstStep
            ? null
            : ReplyKeyboardBuilder.CreateBackButton(dialogContext.DialogName),
            parseMode: ParseMode.MarkdownV2, 
            cancellationToken: cancellationToken);
        return Result.Success();
    }

    public override Task<Result> HandleAsync(ITelegramBotClient client, Update update, DialogContext dialogContext, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: not null })
            return Task.FromResult(
                Result.Failure($"Update type is {update.Type}, expected type is {UpdateType.Message}"));
        try
        {
            var message = update.Message;
            var valueToAdd = (T)Convert.ChangeType(message.Text, typeof(T));
            if (validate != null)
            {
                var validationResult = validate(valueToAdd);
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