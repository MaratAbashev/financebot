using FinBot.Domain.Models;
using Telegram.Bot.Types;
using IRequest = MediatR.IRequest;

namespace FinBot.Bll.Implementation.Requests;

public record ProcessDialogRequest(Update Update, DialogContext DialogContext) : IRequest;

