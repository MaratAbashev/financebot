using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.implementation.Commands.StaticCommands;

[SlashCommand("/help")]
public class HelpCommand(ITelegramBotClient botClient): IStaticCommand
{
    public async Task Handle(Message message)
    {
        await botClient.SendMessage(message.Chat.Id, 
            "Вот что я могу: \n1\\. Считать твой бюджет на день\n2\\. Строить графики того как ты экономишь",
            parseMode: ParseMode.MarkdownV2);
    }
}