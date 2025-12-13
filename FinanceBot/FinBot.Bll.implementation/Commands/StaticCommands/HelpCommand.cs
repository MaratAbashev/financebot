using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/help")]
[TextCommand("Помощь")]
public class HelpCommand(ITelegramBotClient botClient): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await botClient.SendMessage(update.Message!.Chat.Id, 
            "Вот что я могу: \n1\\. Считать твой бюджет на день\n2\\. Строить графики того как ты экономишь",
            parseMode: ParseMode.MarkdownV2);
    }
}