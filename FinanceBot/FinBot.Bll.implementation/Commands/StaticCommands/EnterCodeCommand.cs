using FinBot.Bll.Implementation.Dialogs.Definitions;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/enter_code")]
[TextCommand("Ввести код приглашение")]
public class EnterCodeCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(
            update, 
            nameof(EnterCodeDialog), 
            update.Message!.From!.Id));
    }
}