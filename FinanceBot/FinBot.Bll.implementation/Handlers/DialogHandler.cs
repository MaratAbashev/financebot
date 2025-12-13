using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Handlers;

public class DialogHandler(IGenericRepository<DialogContext, int, PDbContext> dialogRepository,
    IEnumerable<IDialogDefinition> dialogs,
    ITelegramBotClient botClient): IRequestHandler<StartDialogRequest>, IRequestHandler<ProcessDialogRequest>
{
    public async Task Handle(StartDialogRequest request, CancellationToken cancellationToken)
    {
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == request.DialogName);
        if (dialogDefinition == null)
            return;
        var dialogContext = await dialogRepository.FirstOrDefaultAsync(dlg => dlg.UserId == request.UserId) ?? new DialogContext();
        dialogContext.DialogStorage = new Dictionary<string, object>();
        dialogContext.DialogName = request.DialogName;
        dialogContext.UserId = request.UserId;
        dialogContext.CurrentStep = 0;
        dialogContext.PrevStep = -1;
        await dialogDefinition.Steps[dialogContext.CurrentStep]
            .PromptAsync(botClient, request.UserId, dialogContext, cancellationToken);
        if (dialogContext.Id == 0)
        {
            await dialogRepository.AddAsync(dialogContext);
        }
        else
        {
            dialogRepository.Update(dialogContext);
        }
        
        await dialogRepository.SaveChangesAsync();
    }
    
    public async Task Handle(ProcessDialogRequest request, CancellationToken cancellationToken)
    {
        var update = request.Update;
        var dialogContext = request.DialogContext;
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == dialogContext.DialogName);
        if (dialogDefinition == null)
            return;
        if (update.CallbackQuery is { Data: not null } query
            && query.Data.StartsWith("dlg__back"))
        {
            var prevStepIndex = dialogContext.PrevStep;
            if (query.Data.Split('/')[1] != dialogContext.DialogName
                || !dialogDefinition.Steps.TryGetValue(prevStepIndex, out var prevStep))
                return;
            await botClient.AnswerCallbackQuery(query.Id, cancellationToken: cancellationToken);
            dialogContext.CurrentStep = prevStepIndex;
            dialogContext.PrevStep = prevStep.PrevStepId(dialogContext);
            await prevStep
                .PromptAsync(botClient, query.From.Id, dialogContext, cancellationToken);
            await dialogRepository.SaveChangesAsync();
            return;
        }
        
        var handleResult = await dialogDefinition.Steps[dialogContext.CurrentStep]
            .HandleAsync(botClient, update, dialogContext, cancellationToken);
        if (handleResult is { IsSuccess: false, ErrorMessage: not null, ErrorType: ErrorType.Validation })
        {
            await botClient.SendMessage(dialogContext.UserId, 
                handleResult.ErrorMessage,
                parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
            await dialogDefinition.Steps[dialogContext.CurrentStep]
                .PromptAsync(botClient, dialogContext.UserId, dialogContext, cancellationToken);
            await dialogRepository.SaveChangesAsync();
            return;
        }

        if (!handleResult.IsSuccess)
        {
            return;
        }

        var nextStepId = dialogDefinition.Steps[dialogContext.CurrentStep]
            .NextStepId(dialogContext);
        if (dialogDefinition
            .Steps
            .TryGetValue(nextStepId, out var nextStep))
        {
            await nextStep.PromptAsync(botClient, dialogContext.UserId, dialogContext, cancellationToken);
            (dialogContext.PrevStep, dialogContext.CurrentStep) = (dialogContext.CurrentStep, nextStepId);
            await dialogRepository.SaveChangesAsync();
            return;
        }

        if (nextStepId == -1)
        {
            await dialogDefinition.OnCompletedAsync(dialogContext.UserId, dialogContext, cancellationToken);
            dialogRepository.Delete(dialogContext);
            await dialogRepository.SaveChangesAsync();
        }
    }
}