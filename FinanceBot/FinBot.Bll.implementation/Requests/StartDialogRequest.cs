using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.implementation.Requests;

public record StartDialogRequest(Update Update, string DialogName, long UserId): IRequest;