using FinBot.Bll.Interfaces.Services;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/me")]
[TextCommand("Мой Id")]
public class MeCommand(ITelegramBotClient botClient,
    IUserService userService): IStaticCommand
{
    public async Task Handle(Update update)
    {
        var userResult = await userService.GetOrCreateUserAsync(update.Message!.From!.Id, update.Message.From!.FirstName);
        if (!userResult.IsSuccess)
            return;
        var answer = $"Твой Id: `{userResult.Data.Id}`";
        await botClient.SendMessage(update.Message!.Chat.Id, 
            answer,
            parseMode: ParseMode.MarkdownV2
            );
    }
}