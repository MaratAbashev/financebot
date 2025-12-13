using MediatR;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Requests;

public record StartDialogRequest(Update Update, string DialogName, long UserId): IRequest;