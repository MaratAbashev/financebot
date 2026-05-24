using FinBot.Bll.Implementation.Requests;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Services;

public class ReceiverService(ITelegramBotClient botClient, IMediator mediator, ILogger<ReceiverService> logger)
{
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        await botClient.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken ct)
    {
        await mediator.Send(new ProcessTelegramUpdateRequest(update), ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Telegram polling error from {Source}", ex.Source);
        return Task.CompletedTask;
    }
}