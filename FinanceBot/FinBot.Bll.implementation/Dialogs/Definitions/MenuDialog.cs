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
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class MenuDialog : IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IGroupService _groupService;

    public MenuDialog(IMediator mediator,
        IGroupService groupService)
    {
        _mediator = mediator;
        _groupService = groupService;
        Steps = BuildSteps();
    }

    private IReadOnlyDictionary<int, IStep> BuildSteps()
    {
        var builder = new DialogBuilder();
        builder
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
                    .Select(g => (g.Name, g.Id.ToString()))
                    .ToList();

                ctx.DialogStorage!["groupsButtons"] = buttons;

                return Result<IEnumerable<string>>.Success([]);
            })
            .AsFirstStep()
            .Commit();
        builder
            .AddChoiceStep<string>(
                "chooseAction",
                "Что будем делать?\n{{groupBalance}}\n{{accountBalance}}\n{{savingBalance}}",
                ctx =>
                {
                    List<(string ButtonName, string ButtonValue)> buttons =
                    [
                        ("Метрики Excel", nameof(ExcelMetricsDialog)),
                        ("ИИ помощник", nameof(AiHelperDialog)),
                        ("Внести трату", nameof(AddExpenseDialog))
                    ];

                    if (ctx.DialogStorage!.TryGetValue("isAdmin", out var isAdmin) && (bool)isAdmin)
                    {
                        buttons.AddRange([
                            ("Изменить группу", nameof(ConfigureGroupDialog)),
                            ("Изменить копилку", nameof(ChangeGoalDialog)),
                            ("Управление пользователями", nameof(ManageUsersDialog))
                        ]);
                    }

                    return buttons;
                })
            .WithDataLoader(
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                    if (!groupResult.IsSuccess)
                        return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);

                    var group = groupResult.Data;
                    var saving = group.Saving;
                    var account = group.Accounts.SingleOrDefault(a => a.User!.TelegramId == ctx.UserId);

                    ctx.DialogStorage!["savingFlag"] = group.SavingStrategy == SavingStrategy.Save;
                    ctx.DialogStorage!["savingActiveFlag"] = group.Saving!.IsActive;

                    var groupBalance = $"Баланс группы: {Math.Ceiling(group.GroupBalance)} из {Math.Ceiling(group.MonthlyReplenishment)}";
                    var accountBalance = $"Ваш баланс: {Math.Ceiling(account!.Balance)} из {Math.Ceiling(account.DailyAllocation)}";
                    var savingBalance = group.SavingStrategy == SavingStrategy.Save
                        ? $"В копилке {Math.Ceiling(saving!.CurrentAmount)} из {Math.Ceiling(saving.TargetAmount)}"
                        : string.Empty;

                    ctx.DialogStorage!["accountBalance"] = accountBalance;
                    ctx.DialogStorage!["groupBalance"] = groupBalance;
                    ctx.DialogStorage!["savingBalance"] = savingBalance;
                    ctx.DialogStorage!["isAdmin"] = account.Role == Role.Admin;

                    return Result<IEnumerable<string>>.Success(["groupBalance", "accountBalance", "savingBalance"]);
                })
            .Commit();
        return builder.Build();
    }

    public string DialogName => nameof(MenuDialog);

    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext,
        Update update, CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData("chooseAction", out string menuStepStr))
        {
            return;
        }

        await _mediator.Send(new StartDialogRequest(update, menuStepStr, chatId, dialogContext.DialogStorage), cancellationToken);
    }
}