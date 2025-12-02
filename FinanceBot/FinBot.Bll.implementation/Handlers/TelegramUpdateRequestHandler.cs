using FinBot.Bll.Implementation.Requests;
using FinBot.Domain.Utils;
using MediatR;

namespace FinBot.Bll.Implementation.Handlers;

public class TelegramUpdateRequestHandler(IMediator mediator): IRequestHandler<ProcessTelegramUpdateRequest>
{
    public async Task Handle(ProcessTelegramUpdateRequest request, CancellationToken cancellationToken)
    { 
        var update = request.Update;
        if (update.CallbackQuery != null)
        {
            
        }
        if (update.Message is { Text: not null })
        {
            var result = await mediator.Send<Result>(new ProcessMessageCommandRequest(update.Message), cancellationToken);
            if (result.IsSuccess)
                return;
        }
        
    }
}