using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Implementation.Dialogs.Steps;
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

public class AddExpenseDialog: IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IExpenseService _expenseService;
    private readonly IGroupService _groupService;
    private readonly IUserService _userService;
    private readonly ITelegramBotClient _botClient;

    public AddExpenseDialog(IMediator mediator,
        IExpenseService expenseService,
        IGroupService groupService,
        IUserService userService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _expenseService = expenseService;
        _groupService = groupService;
        _userService = userService;
        _botClient = botClient;
        Steps = BuildSteps();
    }

    public string DialogName => nameof(AddExpenseDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder(-1);
        builder.AddChoiceStep<string>(
                "mock",
                "mock",
                _ => [])
            .WithDataLoader(_ => Task.FromResult(Result<IEnumerable<string>>.Failure("mock")))
            .OnPromptFailed(async (_, chatId, update, _) =>
            {
                await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId));
            })
            .Commit();

        builder.AddTextStep<decimal>(
            "expense",
            "Вам доступно {{amount}} руб\nВведите трату (если не целое то через точку)"
                )
            .WithDataLoader(async ctx =>
            {
                if (!Guid.TryParse(ctx.DialogStorage!["chooseGroup"].ToString(), out var groupId))
                    return Result<IEnumerable<string>>.Failure("Cant parse Guid");
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                if (!groupResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);
                var account = groupResult.Data.Accounts
                    .FirstOrDefault(a => a.User?.TelegramId == ctx.UserId);
                if (account == null)
                    return Result<IEnumerable<string>>.Failure("No accounts found");
                ctx.DialogStorage!["amount"] = account.Balance;
                return Result<IEnumerable<string>>.Success(["amount"]);
            })
            .WithValidation(
                value => value > 0m
                    ? Result.Success()
                    : Result.Failure("Введите число больше нуля"))
            .Commit();

        builder.AddChoiceStep<int>(
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

        return builder.Build();
    }

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        var userResult = await _userService.GetUserByTgIdAsync(chatId);
        if (!userResult.IsSuccess)
            return;
        if (!dialogContext.TryGetData<string>( "chooseGroup", out var groupId)
            || !dialogContext.TryGetData<decimal>("expense", out var expense)
            || !dialogContext.TryGetData<int>("expenseCategory", out var expenseCategory)
            || !Guid.TryParse(groupId, out var groupIdGuid))
            return;
        var addExpenseResult = await _expenseService.AddExpenseAsync(chatId, groupIdGuid, expense, (ExpenseCategory)expenseCategory, cancellationToken);

        await _botClient.SendMessage(
            chatId,
            addExpenseResult.IsSuccess
                ? $"Успешно, ваш остаток на сегодня: {addExpenseResult.Data} руб"
                : "Не удалось добавить трату, попробуйте еще раз",
            cancellationToken: cancellationToken);
        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}