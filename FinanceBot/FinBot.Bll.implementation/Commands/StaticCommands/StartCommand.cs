using FinBot.Bll.Interfaces.Services;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/start")]
[TextCommand("Начать")]
public class StartCommand(ITelegramBotClient botClient,
    IUserService userService): IStaticCommand
{
    private readonly ReplyKeyboardMarkup _markup = ReplyKeyboardBuilder
        .CreateKeyboard("Начать")
        .AddKeyboardRow("Помощь")
        .AddKeyboardRow("Мой Id")
        .BuildKeyboardMarkup();
    public async Task Handle(Update update)
    {
        var userResult = await userService.GetOrCreateUserAsync(update.Message!.From!.Id, update.Message.From!.FirstName);
        if (!userResult.IsSuccess)
            return;
        await botClient.SendMessage(update.Message!.Chat.Id, 
            $"Привет, {userResult.Data.DisplayName}, я бот-помощник с финансами. Давай начнем работу.\nВведи /help чтобы увидеть список команд",
            replyMarkup: _markup
            );
    }
}