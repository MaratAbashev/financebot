using FinBot.Bll.implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.implementation.Commands.StaticCommands;

[SlashCommand("/test")]
[TextCommand("Тест")]
public class TestDialogCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(update,"TestDialog", update.Message!.From!.Id));
    }
}