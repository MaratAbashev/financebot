using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;

namespace FinBot.Bll.Implementation.Handlers;

public class TelegramUpdateRequestHandler(IMediator mediator, IGenericRepository<DialogContext, int, PDbContext> repository): IRequestHandler<ProcessTelegramUpdateRequest>
{
    public async Task Handle(ProcessTelegramUpdateRequest request, CancellationToken cancellationToken)
    { 
        var update = request.Update;
        DialogContext? dialog;
        if (update.CallbackQuery is {} callbackQuery)
        {
            dialog = await repository.FirstOrDefaultAsync(d => d.UserId == callbackQuery.From.Id);
            if (dialog != null && callbackQuery.Data != null && callbackQuery.Data.StartsWith("dlg"))
                await mediator.Send(new ProcessDialogRequest(update, dialog), cancellationToken);
            return;
        }
        if (update.Message is { Text: not null } message)
        {
            dialog = await repository.FirstOrDefaultAsync(d => d.UserId == message.From!.Id);
            var result = await mediator.Send<Result>(new ProcessMessageCommandRequest(update), cancellationToken);
            if (result.IsSuccess) //TODO добавить обработку если диалог был а юзер его прервал
                return;
            if (dialog != null)
                await mediator.Send(new ProcessDialogRequest(update, dialog), cancellationToken);
        }
        
    }
}