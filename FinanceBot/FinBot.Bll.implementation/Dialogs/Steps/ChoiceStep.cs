using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinBot.Bll.Implementation.Dialogs.Steps;

public class ChoiceStep<T>(
    string key,
    string promptTemplate,
    Func<DialogContext, int> nextStepId,
    Func<DialogContext, int> prevStepId,
    Func<DialogContext, IEnumerable<(string ButtonName, T ButtonValue)>> buttonMapper,
    Func<DialogContext, Task<Result<IEnumerable<string>>>>? dataLoader = null,
    bool isFirstStep = false,
    Func<Result, long, Update, DialogContext, Task>? onPromptFailed = null)
    : DataStep(key, nextStepId, prevStepId,
        dataLoader, isFirstStep, onPromptFailed) where T: IConvertible
{
    public override async Task<Result> PromptAsync(ITelegramBotClient client, long chatId, DialogContext dialogContext,
        CancellationToken cancellationToken)
    {
        var loadDataResult = await DataLoader(dialogContext);
        if (!loadDataResult.IsSuccess)
            return Result.Failure(loadDataResult.ErrorMessage!);
        var promptResult = FormatPrompt(promptTemplate, dialogContext, loadDataResult.Data);
        if (!promptResult.IsSuccess)
            return Result.Failure(promptResult.ErrorMessage!);
        var buttons = buttonMapper(dialogContext)
            .Select(b =>
                InlineKeyboardButton
                    .WithCallbackData(b.ButtonName, $"dlg/{dialogContext.DialogName}/{dialogContext.CurrentStep}/{b.ButtonValue.ToString()}"))
            .Chunk(1)
            .ToArray();
        var markup = IsFirstStep
            ? buttons.BuildInlineKeyboardMarkup()
            : ReplyKeyboardBuilder.CreateBackButton(dialogContext.DialogName)
                .Concat(buttons)
                .ToArray()
                .BuildInlineKeyboardMarkup();

        await client.SendMessage(chatId, promptResult.Data, replyMarkup: markup,
            parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        return Result.Success();
    }

    public override async Task<Result> HandleAsync(ITelegramBotClient client, Update update, DialogContext dialogContext,
        CancellationToken cancellationToken)
    {
        if (update.CallbackQuery is not { Data: not null })
            return Result.Failure($"Update type is {update.Type}, expected type is {UpdateType.CallbackQuery}");
        try
        {
            var query = update.CallbackQuery;
            var data = query.Data.Split("/");
            if (data is not ["dlg", _, _, _]
                || data[1] != dialogContext.DialogName
                || data[2] != dialogContext.CurrentStep.ToString())
                return Result.Failure("Invalid query", ErrorType.BadRequest);
            var valueToAdd = (T)Convert.ChangeType(data[3], typeof(T));
            if (dialogContext.DialogStorage != null)
                dialogContext.DialogStorage[Key] = valueToAdd;
            await client.AnswerCallbackQuery(query.Id, cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException)
        {
            return Result.Failure("Invalid data cast");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}