using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.implementation.Requests;

public record ProcessTelegramUpdateRequest(Update Update): IRequest;