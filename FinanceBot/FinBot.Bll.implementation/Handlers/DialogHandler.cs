using System.Diagnostics;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
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
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Handlers;

public class DialogHandler(ICacheStorage cacheStorage,
    IEnumerable<IDialogDefinition> dialogs,
    ITelegramBotClient botClient,
    BusinessMetrics metrics): IRequestHandler<StartDialogRequest>, IRequestHandler<ProcessDialogRequest>
{
    public async Task Handle(StartDialogRequest request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySources.FinBot.StartActivity("dialog.start", ActivityKind.Internal);
        activity?.SetTag(ObservabilityConstants.Tags.DialogType, request.DialogName);
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == request.DialogName);
        if (dialogDefinition == null)
            return;
        var dialogContext = await cacheStorage.GetAsync<DialogContext>(request.UserId.ToString()) ?? new DialogContext();
        dialogContext.DialogStorage = request.CommonContext ?? new Dictionary<string, object>();
        dialogContext.DialogName = request.DialogName;
        dialogContext.UserId = request.UserId;
        dialogContext.CurrentStep = 0;
        dialogContext.PrevStep = -1;
        var step = dialogDefinition.Steps[dialogContext.CurrentStep];
        if (!await TryPrompt(request.UserId, step, request.Update, dialogContext, cancellationToken))
            return;

        await cacheStorage.SetAsync(request.UserId.ToString(), dialogContext, TimeSpan.FromMinutes(10));
    }

    public async Task Handle(ProcessDialogRequest request, CancellationToken cancellationToken)
    {
        var update = request.Update;
        var dialogContext = request.DialogContext;
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == dialogContext.DialogName);
        if (dialogDefinition == null)
            return;

        using var activity = ActivitySources.FinBot.StartActivity("dialog.step", ActivityKind.Internal);
        activity?.SetTag(ObservabilityConstants.Tags.DialogType, dialogContext.DialogName);
        activity?.SetTag(ObservabilityConstants.Tags.DialogStep, dialogContext.CurrentStep);
        var stepStopwatch = Stopwatch.StartNew();
        if (update.CallbackQuery is { Data: not null } query
            && query.Data.StartsWith("dlg__back"))
        {
            var prevStepIndex = dialogContext.PrevStep;
            if (query.Data.Split('/')[1] != dialogContext.DialogName
                || !dialogDefinition.Steps.TryGetValue(prevStepIndex, out var prevStep))
                return;
            await botClient.AnswerCallbackQuery(query.Id, cancellationToken: cancellationToken);
            dialogContext.DialogStorage?.Remove(dialogDefinition.Steps[dialogContext.CurrentStep].Key);
            dialogContext.CurrentStep = prevStepIndex;
            dialogContext.PrevStep = prevStep.PrevStepId(dialogContext);
            if (!await TryPrompt(query.From.Id, prevStep, update, dialogContext, cancellationToken))
                return;
            await cacheStorage.SetAsync(dialogContext.UserId.ToString(), dialogContext, TimeSpan.FromMinutes(10));
            return;
        }

        var handleStep = dialogDefinition.Steps[dialogContext.CurrentStep];
        var handleResult = await handleStep
            .HandleAsync(botClient, update, dialogContext, cancellationToken);
        if (handleResult is { IsSuccess: false, ErrorMessage: not null, ErrorType: ErrorType.Validation })
        {
            await botClient.SendMessage(dialogContext.UserId, 
                handleResult.ErrorMessage,
                parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
            if (!await TryPrompt(dialogContext.UserId, handleStep, update, dialogContext, cancellationToken))
                return;
            await cacheStorage.SetAsync(dialogContext.UserId.ToString(), dialogContext, TimeSpan.FromMinutes(10));
            return;
        }

        if (!handleResult.IsSuccess)
        {
            return;
        }

        var nextStepId = handleStep
            .NextStepId(dialogContext);
        if (dialogDefinition
            .Steps
            .TryGetValue(nextStepId, out var nextStep))
        {
            var prevStep = dialogContext.PrevStep;
            (dialogContext.PrevStep, dialogContext.CurrentStep) = (dialogContext.CurrentStep, nextStepId);
            if (!await TryPrompt(dialogContext.UserId, nextStep, update, dialogContext, cancellationToken))
            {
                (dialogContext.PrevStep, dialogContext.CurrentStep) = (prevStep, dialogContext.PrevStep);
                return;
            }
            await cacheStorage.SetAsync(dialogContext.UserId.ToString(), dialogContext, TimeSpan.FromMinutes(10));
            return;
        }

        if (nextStepId == -10)
        {
            await dialogDefinition.OnCompletedAsync(dialogContext.UserId, dialogContext, update, cancellationToken);
            stepStopwatch.Stop();
            metrics.DialogCompletionDuration.Record(
                stepStopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("dialog_type", dialogContext.DialogName));
        }
    }
    
    private async Task<bool> TryPrompt(long userId, IStep step, Update update,
        DialogContext dialogContext, CancellationToken cancellationToken)
    {
        var promptResult = await step
            .PromptAsync(botClient, userId, dialogContext, cancellationToken);
        if (promptResult.IsSuccess) return true;
        if (step.OnPromptFailed != null)
            await step.OnPromptFailed(promptResult, userId, update, dialogContext);
        return false;
    }
}