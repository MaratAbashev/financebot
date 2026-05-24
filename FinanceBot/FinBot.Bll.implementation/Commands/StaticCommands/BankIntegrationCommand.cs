using FinBot.Bll.Implementation.Dialogs.Definitions;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/bank_integration")]
[TextCommand("Интеграция с банком")]
public class BankIntegrationCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(
            update, 
            nameof(BankIntegrationDialog), 
            update.Message!.From!.Id));
    }
}