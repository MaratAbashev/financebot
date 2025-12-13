using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Requests;

public record ProcessMessageCommandRequest(Update Update): IRequest, IRequest<Result>;