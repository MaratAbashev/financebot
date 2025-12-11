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
        var update = request.Update;
        if (staticCommands.TryGetValue(update.Message!.Text!, out var command))
        {
            await command.Handle(update);
            return Result.Success();
        }

        var expPattern = regExpCommands.Keys.FirstOrDefault(pattern => Regex.IsMatch(update.Message!.Text!, pattern));
        if (expPattern == null) 
            return Result.Failure("Unknown command", ErrorType.NotFound);
        await regExpCommands[expPattern].Handle(update);
        return Result.Success();
    }
}