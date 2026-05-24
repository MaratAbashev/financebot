using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class EnterCodeDialog: IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IInvitationService _invitationService;
    private readonly ITelegramBotClient _botClient;
    public EnterCodeDialog(IMediator mediator,
        IUserService userService,
        IInvitationService invitationService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _userService = userService;
        _invitationService = invitationService;
        _botClient = botClient;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder();
        builder.AddTextStep<string>(
                "code",
                "Введите код-приглашение")
            .AsFirstStep()
            .Commit();
        return builder.Build();
    }
    public string DialogName => nameof(EnterCodeDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        var getUserResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!getUserResult.IsSuccess)
            return;
        
        var user = getUserResult.Data;
        
        if (!dialogContext.TryGetData<string>("code", out var code))
            return;
        
        var sendRequestResult = await _invitationService.JoinGroupByCodeAsync(chatId, code, cancellationToken);
        if (!sendRequestResult.IsSuccess)
        {
            var message = sendRequestResult.ErrorType switch
            {
                ErrorType.Conflict => "Пользователь уже отправил заявку или состоит в группе",
                ErrorType.Validation => "Код не действителен",
                _ => "Ошибка при обработке заявки"
            };

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
        }
        else
        {
            var group = sendRequestResult.Data;
            var groupCreatorTgId = group.Creator!.TelegramId;
            await _botClient.SendMessage(
                groupCreatorTgId,
                $"В вашу группу {group.Name} хочет добавиться {user.DisplayName}",
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            await _botClient.SendMessage(
                chatId,
                "Заявка отправлена создателю группы",
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
        }

        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}