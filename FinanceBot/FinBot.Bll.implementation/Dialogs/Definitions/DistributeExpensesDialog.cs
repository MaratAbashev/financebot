using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Utils;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class DistributeExpensesDialog : IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IExpenseService _expenseService;
    private readonly ITelegramBotClient _botClient;

    public DistributeExpensesDialog(
        IMediator mediator,
        IGroupService groupService,
        IUserService userService,
        IExpenseService expenseService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _groupService = groupService;
        _userService = userService;
        _expenseService = expenseService;
        _botClient = botClient;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder();
        var expensesStep = builder
            .AddChoiceStep<int>(
                "expense",
                "Выберите трату",
                ctx =>
                {
                    var buttons = ctx.DialogStorage!["pendingExpensesButtons"];
                    return (IEnumerable<(string ButtonName, int ButtonValue)>)buttons;
                })
            .WithDataLoader(async ctx =>
            {
                var expensesResult = await _expenseService.GetPendingExpensesAsync(ctx.UserId);
                if (!expensesResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(expensesResult.ErrorMessage!, expensesResult.ErrorType);

                var buttons = expensesResult.Data
                    .Select(e => ($"{e.Date:yyyy-MM-dd}: {e.Amount}", e.Id))
                    .ToList();

                ctx.DialogStorage!["pendingExpensesButtons"] = buttons;

                return Result<IEnumerable<string>>.Success([]);
            })
            .AsFirstStep()
            .Commit();

        var chooseGroupStep = builder
            .AddChoiceStep(
                "chooseGroup",
                "Выберите группу",
                ctx =>
                {
                    var buttons = ctx.DialogStorage!["groupsButtons"];

                    return (IEnumerable<(string ButtonName, string ButtonValue)>)buttons;
                })
            .WithDataLoader(async ctx =>
            {
                var groupsResult = await _groupService.GetUserGroupsAsync(ctx.UserId, adminOnly: false);
                if (!groupsResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupsResult.ErrorMessage!, groupsResult.ErrorType);

                var buttons = groupsResult.Data
                    .Select(g => (g.Name, g.Id.ToString("N")))
                    .ToList();

                buttons.Add(("Отказаться", "reject"));

                ctx.DialogStorage!["groupsButtons"] = buttons;

                return Result<IEnumerable<string>>.Success([]);
            })
            .Commit();

        var chooseCategoryStep = builder.AddChoiceStep<int>(
                "expenseCategory",
                "Выберите категорию траты",
                _ =>
                [
                    ("Еда", (int)ExpenseCategory.Food),
                    ("Транспорт", (int)ExpenseCategory.Transport),
                    ("Коммунальные услуги", (int)ExpenseCategory.Housing),
                    ("Шоппинг", (int)ExpenseCategory.Shopping),
                    ("Развлечения", (int)ExpenseCategory.Entertainment),
                    ("Здоровье", (int)ExpenseCategory.Health),
                    ("Другое", (int)ExpenseCategory.Other),
                ])
            .Commit();

        expensesStep.GoTo(chooseGroupStep);
        chooseGroupStep.GoTo(ctx =>
        {
            ctx.TryGetData<string>("chooseGroup", out var choice);
            if (Guid.TryParse(choice, out _)) return chooseCategoryStep.Id;
            ctx.DialogStorage!["expenseCategory"] = 0;
            return -10;
        });
        chooseCategoryStep.GoTo(_ => -10);

        return builder.Build();
    }

    public string DialogName => nameof(DistributeExpensesDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        var getUserResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!getUserResult.IsSuccess)
            return;

        if (!dialogContext.TryGetData<string>("chooseGroup", out var chooseGroup)
            || !dialogContext.TryGetData<int>("expense", out var expenseId)
            || !dialogContext.TryGetData<int>("expenseCategory", out var expenseCategory))
            return;

        string message;
        if (chooseGroup.Equals("reject"))
        {
            var rejectResult = await _expenseService.DistributeExpensesAsync(
                chatId,
                expenseId,
                null,
                null,
                true,
                cancellationToken);
            message = rejectResult.IsSuccess
                ? "Трата успешно отменена"
                : "Не удалось отменить трату, попробуйте позже";
        }
        else
        {
            if (!Guid.TryParse(chooseGroup, out var groupId))
                return;

            var distributeResult = await _expenseService.DistributeExpensesAsync(chatId,
                expenseId, groupId, (ExpenseCategory)expenseCategory, cancellationToken: cancellationToken);
            message = distributeResult.IsSuccess
                ? "Трата успешно распределена"
                : "Не удалось распределить трату, попробуйте еще раз";
        }


        await _botClient.SendMessage(
            chatId,
            message,
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        await _mediator.Send(new StartDialogRequest(update, DialogName, chatId), cancellationToken);
    }
}