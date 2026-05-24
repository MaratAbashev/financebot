using FinBot.Bll.Implementation.Dialogs.Definitions;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/create_group")]
[TextCommand("Создать группу")]
public class CreateGroupCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(
            update, 
            nameof(CreateGroupDialog), 
            update.Message!.From!.Id));
    }
}