using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class BankIntegrationDialog : IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IBankServiceClient _bankServiceClient;
    private readonly IInternalBankService _internalBankService;
    private readonly IUserService _userService;
    private readonly ITelegramBotClient _botClient;

    public BankIntegrationDialog(IBankServiceClient bankServiceClient,
        IMediator mediator,
        IInternalBankService internalBankService,
        IUserService userService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _bankServiceClient = bankServiceClient;
        _internalBankService = internalBankService;
        _userService = userService;
        _botClient = botClient;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder();
        var chooseActionStep = builder
            .AddChoiceStep<int>("action",
                "Что вы хотите сделать?",
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Подключения", 1),
                    ];
                    if (ctx.DialogStorage!.TryGetValue("connected", out var connected)
                        && (bool)connected)
                        buttons.Add(("Синхронизировать траты", 0));
                    return buttons;
                }
            )
            .WithDataLoader(async ctx =>
            {
                var connectedResult =
                    await _internalBankService.IsBankConnectedAsync(ctx.UserId, ct: CancellationToken.None);
                if (!connectedResult.IsSuccess)
                    Result<IEnumerable<string>>.Failure(connectedResult.ErrorMessage!, connectedResult.ErrorType);
                ctx.DialogStorage!["connected"] = connectedResult.Data;

                return Result<IEnumerable<string>>.Success([]);
            })
            .AsFirstStep()
            .Commit();
        var chooseConnectionStep = builder.AddChoiceStep<int>(
                "connectToBank",
                "Что вы хотите сделать?",
                _ =>
                [
                    ("Отключиться", 0),
                    ("Подключиться", 1),
                ])
            .Commit();

        chooseActionStep.GoTo(ctx =>
        {
            ctx.TryGetData<int>("action", out var action);
            if (action != 0) return chooseConnectionStep.Id;
            ctx.DialogStorage!["connectToBank"] = -1;
            return -10;
        });

        chooseConnectionStep.Back(chooseActionStep);

        return builder.Build();
    }

    public string DialogName => nameof(BankIntegrationDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData<int>("action", out var action)
            || !dialogContext.TryGetData<int>("connectToBank", out var connectToBank)
            || !dialogContext.TryGetData<bool>("connected", out var connected))
            return;

        var userResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!userResult.IsSuccess)
            return;
        var user = userResult.Data;
        string message;

        if (connected)
        {
            if (action == 0)
            {
                var sychronizeResult =
                    await _bankServiceClient.SynchronizeTransactionsAsync(user.Id, CancellationToken.None);
                message = !sychronizeResult.IsSuccess
                    ? "Ошибка при синхронизации трат, попробуйте позже"
                    : sychronizeResult.Data == 0
                        ? "Новые траты отсутствуют"
                        : $"Из банка пришло {sychronizeResult.Data} новых трат.\nРаспределите новые траты";
            }
            else
            {
                if (connectToBank == 0)
                {
                    var unlinkResult = await _bankServiceClient.UnlinkBankAsync(user.Id, CancellationToken.None);
                    message = !unlinkResult.IsSuccess
                        ? "Ошибка при отвязке банка, попробуйте позже"
                        : "Банк отвязан";
                }
                else
                {
                    message = "Вы уже привязаны к банку!";
                }
            }
        }
        else
        {
            if (action == 0)
            {
                message = "Вы не привязаны к банку!";
            }
            else
            {
                if (connectToBank == 1)
                {
                    var linkResult = await _bankServiceClient.GetAuthUrlAsync(user.Id, CancellationToken.None);
                    message = !linkResult.IsSuccess
                        ? "Ошибка при привязке банка, попробуйте позже"
                        : $"Ссылка для привязки банка: {linkResult.Data}";
                }
                else
                {
                    message = "Вы не привязаны к банку!";
                }
            }
        }

        await _botClient.SendMessage(
            chatId,
            message.EscapeMarkdownV2(),
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}