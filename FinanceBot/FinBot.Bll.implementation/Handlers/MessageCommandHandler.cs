using System.Text.RegularExpressions;
using FinBot.Bll.implementation.Requests;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Utils;
using MediatR;

namespace FinBot.Bll.implementation.Handlers;

public class MessageCommandHandler( 
    Dictionary<string, IStaticCommand> staticCommands,
    Dictionary<string, IRegExpCommand> regExpCommands): IRequestHandler<ProcessMessageCommandRequest, Result>
{
    public async Task<Result> Handle(ProcessMessageCommandRequest request, CancellationToken cancellationToken)
    {
        var message = request.Message;
        if (staticCommands.TryGetValue(message.Text!, out var command))
        {
            await command.Handle(message);
            return Result.Success();
        }

        var expPattern = regExpCommands.Keys.FirstOrDefault(pattern => Regex.IsMatch(message.Text!, pattern));
        if (expPattern == null) 
            return Result.Failure("Unknown command", ErrorType.NotFound);
        await regExpCommands[expPattern].Handle(message);
        return Result.Success();
    }
}