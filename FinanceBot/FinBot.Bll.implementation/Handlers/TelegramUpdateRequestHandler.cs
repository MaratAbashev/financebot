using System.Diagnostics;
using FinBot.Bll.Implementation.Requests;
using FinBot.Cache;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using FinBot.Observability.Constants;
using FinBot.Observability.Metrics;
using FinBot.Observability.Tracing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace FinBot.Bll.Implementation.Handlers;

public class TelegramUpdateRequestHandler(
    IMediator mediator,
    ICacheStorage cacheStorage,
    BusinessMetrics metrics,
    ITelegramBotClient botClient) : IRequestHandler<ProcessTelegramUpdateRequest>
{
    public async Task Handle(ProcessTelegramUpdateRequest request, CancellationToken cancellationToken)
    {
        var update = request.Update;

        using var activity = ActivitySources.FinBot.StartActivity("telegram.update", ActivityKind.Server);
        activity?.SetTag(ObservabilityConstants.Tags.TelegramUpdateId, update.Id);

        var updateType = update.Type.ToString();
        metrics.TelegramUpdatesTotal.Add(1, new KeyValuePair<string, object?>("update_type", updateType));

        DialogContext? dialog;
        if (update.CallbackQuery is {} callbackQuery)
        {
            activity?.SetTag(ObservabilityConstants.Tags.TelegramUserId, callbackQuery.From.Id);
            activity?.SetTag(ObservabilityConstants.Tags.TelegramChatId, callbackQuery.Message?.Chat.Id);

            dialog = await cacheStorage.GetAsync<DialogContext>(callbackQuery.From.Id.ToString());
            if (dialog != null && callbackQuery.Data != null && callbackQuery.Data.StartsWith("dlg"))
                await mediator.Send(new ProcessDialogRequest(update, dialog), cancellationToken);
            return;
        }
        if (update.Message is { Text: not null } message)
        {
            activity?.SetTag(ObservabilityConstants.Tags.TelegramUserId, message.From?.Id);
            activity?.SetTag(ObservabilityConstants.Tags.TelegramChatId, message.Chat.Id);

            dialog = await cacheStorage.GetAsync<DialogContext>(message.From!.Id.ToString());
            var result = await mediator.Send<Result>(new ProcessMessageCommandRequest(update), cancellationToken);
            if (result.IsSuccess) //TODO добавить обработку если диалог был а юзер его прервал
                return;
            if (dialog != null)
                await mediator.Send(new ProcessDialogRequest(update, dialog), cancellationToken);
            else
                await botClient.SendMessage(message.From!.Id, "Неизвестная команда", cancellationToken: cancellationToken);
        }

    }
}