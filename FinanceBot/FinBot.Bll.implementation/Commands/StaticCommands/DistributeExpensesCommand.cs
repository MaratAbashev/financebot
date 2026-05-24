using FinBot.Bll.Implementation.Dialogs.Definitions;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Commands.StaticCommands;

[SlashCommand("/distribute_expenses")]
[TextCommand("Распределить траты")]
public class DistributeExpensesCommand(IMediator mediator): IStaticCommand
{
    public async Task Handle(Update update)
    {
        await mediator.Send(new StartDialogRequest(
            update, 
            nameof(DistributeExpensesDialog), 
            update.Message!.From!.Id));
    }
}