using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Requests;

public record ProcessTelegramUpdateRequest(Update Update): IRequest;