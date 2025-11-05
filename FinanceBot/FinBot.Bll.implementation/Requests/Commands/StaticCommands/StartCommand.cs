using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinBot.Bll.implementation.Requests.Commands.StaticCommands;

[SlashCommand("/start")]
[TextCommand("Начать")]
public class StartCommand(ITelegramBotClient botClient): IStaticCommand
{
    private readonly ReplyKeyboardMarkup _markup = new(new KeyboardButton("Начать"));
    public async Task Handle(Message message)
    {
        ReplyKeyboardMarkup replyKeyboardMarkup = new();
        await botClient.SendMessage(message.Chat.Id, 
            "Привет, я бот-помощник с финансами. Давай начнем работу",
            replyMarkup: _markup
            );
    }
}