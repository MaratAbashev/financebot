using Telegram.Bot.Types.ReplyMarkups;

namespace FinBot.Domain.Utils;

public static class ReplyKeyboardBuilder
{
    public static InlineKeyboardButton[][] CreateInlineKeyboard(params InlineKeyboardButton[] buttons)
    {
        return [buttons];
    }

    public static InlineKeyboardButton[][] AddInlineKeyboardRow(this InlineKeyboardButton[][] existingButtonRows,
        params InlineKeyboardButton[] buttonsToAdd)
    {
        return existingButtonRows.Concat([buttonsToAdd]).ToArray();
    }
    
    public static InlineKeyboardMarkup BuildInlineKeyboardMarkup(this InlineKeyboardButton[][] buttonRows)
    {
        return new InlineKeyboardMarkup(buttonRows);
    }

    public static KeyboardButton[][] CreateKeyboard(params KeyboardButton[] buttons)
    {
        return [buttons];
    }

    public static InlineKeyboardButton[][] CreateBackButton(string dialogName, int backStepId)
    {
        return CreateInlineKeyboard(new InlineKeyboardButton("Назад", $"dlg__back/{dialogName}/{backStepId}"));
    }

    public static KeyboardButton[][] AddKeyboardRow(this KeyboardButton[][] existingButtonRows,
        params KeyboardButton[] buttonsToAdd)
    {
        return existingButtonRows.Concat([buttonsToAdd]).ToArray();
    }

    public static ReplyKeyboardMarkup BuildKeyboardMarkup(this KeyboardButton[][] buttonRows, 
        ReplyKeyboardOptions? options = null)
    {
        var customOptions = options ?? ReplyKeyboardOptions.DefaultOptions;
        return new ReplyKeyboardMarkup(buttonRows)
        {
            ResizeKeyboard = customOptions.ResizeKeyboard,
            OneTimeKeyboard = customOptions.OneTimeKeyboard,
            IsPersistent = customOptions.IsPersistent,
            InputFieldPlaceholder = customOptions.InputFieldPlaceholder,
            Selective = customOptions.Selective
        };
    }
}