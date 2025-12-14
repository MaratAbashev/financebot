using FinBot.Bll.Implementation.Dialogs;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;


[SlashCommand("/create")]
[TextCommand("Create")]
public class CreateGroupDialogCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(update,nameof(CreateGroupDialog), update.Message!.From!.Id));
    }
}