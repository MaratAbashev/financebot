using FinBot.Bll.implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using MediatR;

namespace FinBot.Bll.implementation.Handlers;

public class TelegramUpdateRequestHandler(IMediator mediator, Dictionary<string, IStaticCommand> commandMap): IRequestHandler<ProcessTelegramUpdateRequest>
{
    public async Task Handle(ProcessTelegramUpdateRequest request, CancellationToken cancellationToken)
    { 
        var update = request.Update;
        if (update is { Message: {Text: { } text} })
        {
            var message = update.Message;
            if (commandMap.TryGetValue(text, out var command))
            {
                await command.Handle(message);
            }
        }
    }
}