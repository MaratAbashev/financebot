using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using FinBot.Domain.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinBot.Bll.implementation.Commands.StaticCommands;

[SlashCommand("/start")]
[TextCommand("Начать")]
public class StartCommand(ITelegramBotClient botClient): IStaticCommand
{
    private readonly ReplyKeyboardMarkup _markup = ReplyKeyboardBuilder
        .CreateKeyboard("Начать")
        .AddKeyboardRow("Помощь")
        .BuildKeyboardMarkup();
    public async Task Handle(Update update)
    {
        await botClient.SendMessage(update.Message!.Chat.Id, 
            "Привет, я бот-помощник с финансами. Давай начнем работу",
            replyMarkup: _markup
            );
    }
}