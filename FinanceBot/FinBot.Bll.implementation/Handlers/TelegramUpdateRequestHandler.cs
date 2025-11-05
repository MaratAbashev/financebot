using FinBot.Bll.implementation.Requests;
using FinBot.Domain.Utils;
using MediatR;

namespace FinBot.Bll.implementation.Handlers;

public class TelegramUpdateRequestHandler(IMediator mediator): IRequestHandler<ProcessTelegramUpdateRequest>
{
    public async Task Handle(ProcessTelegramUpdateRequest request, CancellationToken cancellationToken)
    { 
        var update = request.Update;
        if (update.Message is { Text: not null })
        {
            var result = await mediator.Send<Result>(new ProcessMessageCommandRequest(update.Message), cancellationToken);
            if (result.IsSuccess)
                return;
        }
    }
}