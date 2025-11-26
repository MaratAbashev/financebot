using Telegram.Bot.Types;

namespace FinBot.Bll.Interfaces.TelegramCommands;

public interface IStaticCommand
{
    Task Handle(Message message);
}