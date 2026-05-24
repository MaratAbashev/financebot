using System.Text;
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

namespace FinBot.Bll.Implementation.Dialogs.Definitions;

public class ManageUsersDialog: IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IGroupService _groupService;
    private readonly IUserService _userService;
    private readonly IInvitationService _invitationService;
    private readonly ITelegramBotClient _botClient;

    public ManageUsersDialog(IMediator mediator,
        IGroupService groupService,
        IUserService userService,
        IInvitationService invitationService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _groupService = groupService;
        _userService = userService;
        _invitationService = invitationService;
        _botClient = botClient;
        Steps = BuildSteps();
    }

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

        var manageMenuStep = builder.AddChoiceStep<int>(
                "manageUsersAction",
                "Управление пользователями",
                _ =>
                [
                    ("Перераспределить выплаты", 0),
                    ("Создать код-приглашение", 1),
                    ("Запросы на вступление", 2),
                    ("Удалить пользователя из группы", 3)
                ])
            .Commit();

        var chooseUserToAddStep = builder.AddChoiceStep(
            "chooseUserToAdd",
            "Пользователи, ожидающие добавления",
            ctx =>
            {
                var buttons = ctx.DialogStorage!["usersToAdd"];
                return (IEnumerable<(string ButtonName, long ButtonValue)>)buttons;
            })
            .WithDataLoader(
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var pendingResult = await _invitationService.GetPendingUsersAsync(groupId);
                    if (!pendingResult.IsSuccess)
                        return Result<IEnumerable<string>>.Failure(pendingResult.ErrorMessage!, pendingResult.ErrorType);

                    var buttons = pendingResult.Data
                        .Select(u => (u.DisplayName, u.TelegramId)).ToList();
                    buttons.Add(("Отклонить всех", -1));

                    ctx.DialogStorage!["usersToAdd"] = buttons;

                    return Result<IEnumerable<string>>.Success([]);
                })
            .Commit();

        var recalculateWithNewUserStep = builder.AddTextStep<string>(
                "recalculateAllocationsWithNewUser",
                "Распределите {{monthlyForRecalculate}} на \n{{usersString}}\nвведите числа через пробел")
            .WithDataLoader(async ctx =>
            {
                var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                if (!groupResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);

                var newUserId = (long) ctx.DialogStorage!["chooseUserToAdd"];
                var newUserResult = await _userService.GetUserByTgIdAsync(newUserId);
                if (!newUserResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(newUserResult.ErrorMessage!, newUserResult.ErrorType);

                var group = groupResult.Data;
                var newUser = newUserResult.Data;
                var userAccounts = group.Accounts.OrderBy(a => a.User!.TelegramId).ToList();
                var accountCount = userAccounts.Count;
                var usersString = new StringBuilder();
                usersString.AppendLine($"1 {newUser.DisplayName}");

                for (var i = 0; i < accountCount; i++)
                {
                    var account = userAccounts[i];
                    usersString.AppendLine($"{i + 2} {account.User!.DisplayName}");
                }

                ctx.DialogStorage!["monthlyForRecalculate"] = group.MonthlyReplenishment;
                ctx.DialogStorage!["usersString"] = usersString.ToString();

                return Result<IEnumerable<string>>.Success(["monthlyForRecalculate", "usersString"]);
            })
            .WithValidation(value =>
            {
                return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                    ? Result.Success()
                    : Result.Failure("Введите через пробел то количество денег которое должно выделяться на каждого пользователя в течение месяца");
            })
            .Commit();
        chooseUserToAddStep.GoTo(ctx =>
        {
            ctx.TryGetData<long>("chooseUserToAdd", out var userToAdd);
            if (userToAdd >= 1) return recalculateWithNewUserStep.Id;
            return -10;
        });
        recalculateWithNewUserStep.GoTo(_ => -10);

        var chooseUserToDeleteStep = builder.AddChoiceStep(
            "chooseRemoveUser",
            "Выберите пользователя на удаление",
            ctx =>
            {
                var buttons = ctx.DialogStorage!["usersToDelete"];

                return (IEnumerable<(string ButtonName, long ButtonValue)>)buttons;
            })
            .WithDataLoader(
            async ctx =>
            {
                var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                if (!groupResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);

                var buttons = groupResult.Data.Accounts.Where(a => a.Role != Role.Admin)
                    .Select(a => (a.User!.DisplayName, a.User.TelegramId)).ToList();

                ctx.DialogStorage!["usersToDelete"] = buttons;

                return Result<IEnumerable<string>>.Success([]);
            })
            .Commit();
        chooseUserToDeleteStep.Back(manageMenuStep);

        var recalculateWithDeletedUserStep = builder.AddTextStep<string>(
                "recalculateAllocationsWithDeletedUser",
                "Распределите {{monthlyForRecalculate}} на \n{{usersString}}\nвведите числа через пробел")
            .WithDataLoader(async ctx =>
            {
                var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                if (!groupResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);

                var group = groupResult.Data;
                var removedUserId = (long)ctx.DialogStorage!["chooseRemoveUser"];
                var userAccounts = group.Accounts
                    .Where(a => a.User!.TelegramId != removedUserId).OrderBy(a => a.User!.TelegramId).ToList();
                var accountCount = userAccounts.Count;
                var usersString = new StringBuilder();

                for (var i = 0; i < accountCount; i++)
                {
                    var account = userAccounts[i];
                    usersString.AppendLine($"{i + 1} {account.User!.DisplayName}");
                }

                ctx.DialogStorage!["accountCount"] = accountCount;
                ctx.DialogStorage!["monthlyForRecalculate"] = group.MonthlyReplenishment;
                ctx.DialogStorage!["usersString"] = usersString.ToString();

                return Result<IEnumerable<string>>.Success(["monthlyForRecalculate", "usersString"]);
            })
            .WithValidation(value =>
            {
                return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                    ? Result.Success()
                    : Result.Failure("Введите через пробел то количество денег которое должно выделяться на каждого пользователя в течение месяца");
            })
            .Commit();
        recalculateWithDeletedUserStep.GoTo(_ => -10);

        var recalculateAllocationsStep = builder.AddTextStep<string>(
                "recalculateAllocations",
                "Распределите {{monthlyForRecalculate}} на \n{{usersString}}\nвведите числа через пробел")
            .WithDataLoader(async ctx =>
            {
                var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                if (!groupResult.IsSuccess)
                    return Result<IEnumerable<string>>.Failure(groupResult.ErrorMessage!, groupResult.ErrorType);

                var group = groupResult.Data;
                var userAccounts = group.Accounts.OrderBy(a => a.User!.TelegramId).ToList();
                var accountCount = userAccounts.Count;
                var usersString = new StringBuilder();

                for (var i = 0; i < accountCount; i++)
                {
                    var account = userAccounts[i];
                    usersString.AppendLine($"{i + 1} {account.User!.DisplayName}");
                }

                ctx.DialogStorage!["accountCount"] = accountCount;
                ctx.DialogStorage!["monthlyForRecalculate"] = group.MonthlyReplenishment;
                ctx.DialogStorage!["usersString"] = usersString.ToString();

                return Result<IEnumerable<string>>.Success(["monthlyForRecalculate", "usersString"]);
            })
            .WithValidation(value =>
            {
                return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                    ? Result.Success()
                    : Result.Failure("Введите через пробел то количество денег которое должно выделяться на каждого пользователя в течение месяца");
            })
            .Commit();
        recalculateAllocationsStep.Back(manageMenuStep);
        manageMenuStep.GoTo(ctx =>
        {
            if (!ctx.TryGetData("manageUsersAction", out int action))
                return manageMenuStep.Id;
            return action switch
            {
                0 => recalculateAllocationsStep.Id,
                1 => -10,
                2 => chooseUserToAddStep.Id,
                3 => chooseUserToDeleteStep.Id,
                _ => manageMenuStep.Id
            };
        });

        return builder.Build();
    }

    public string DialogName => nameof(ManageUsersDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData<string>("chooseGroup", out var groupIdStr)
            || !Guid.TryParse(groupIdStr, out var groupId)
            || !dialogContext.TryGetData<int>("manageUsersAction", out var action))
            return;

        switch (action)
        {
            case 0:
                await HandleRecalculateAsync(chatId, dialogContext, groupId, cancellationToken);
                break;
            case 1:
                await HandleGenerateInviteCodeAsync(chatId, groupId, cancellationToken);
                break;
            case 2:
                await HandleAddUserAsync(chatId, dialogContext, groupId, cancellationToken);
                break;
            case 3:
                await HandleRemoveUserAsync(chatId, dialogContext, groupId, cancellationToken);
                break;
            default:
                return;
        }

        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }

    private async Task HandleRecalculateAsync(long chatId, DialogContext dialogContext, Guid groupId, CancellationToken cancellationToken)
    {
        var groupResult = await _groupService.GetGroupByIdAsync(groupId);
        if (!groupResult.IsSuccess)
            return;

        var group = groupResult.Data;
        
        if (!dialogContext.TryGetData<string>("recalculateAllocations", out var allocationsStr))
            return;

        var allocations = allocationsStr.Split(' ')
            .Select(decimal.Parse)
            .ToArray();

        if (allocations.Sum() != group.MonthlyReplenishment)
        {
            await _botClient.SendMessage(
                chatId,
                $"Ошибка при распределении бюджета. Сумма всех значений должна равняться {group.MonthlyReplenishment}",
                cancellationToken: cancellationToken);
            return;
        }


        var result = await _groupService.RecalculateMonthlyAllocationsAsync(groupId, allocations);
        await _botClient.SendMessage(
            chatId,
            result.IsSuccess
                ? "Распределение успешно обновлено"
                : "Не удалось пересчитать распределение, попробуйте позже",
            cancellationToken: cancellationToken);
    }

    private async Task HandleGenerateInviteCodeAsync(long chatId, Guid groupId, CancellationToken cancellationToken)
    {
        var result = await _invitationService.GenerateInviteCodeAsync(groupId, cancellationToken);
        await _botClient.SendMessage(
            chatId,
            result.IsSuccess
                ? $"Код-приглашение: {result.Data}"
                : "Не удалось создать код-приглашение, попробуйте позже",
            cancellationToken: cancellationToken);
    }

    private async Task HandleAddUserAsync(long chatId, DialogContext dialogContext, Guid groupId,
        CancellationToken cancellationToken)
    {
        var groupResult = await _groupService.GetGroupByIdAsync(groupId);
        if (!groupResult.IsSuccess)
            return;

        var group = groupResult.Data;

        if (!dialogContext.TryGetData<long>("chooseUserToAdd", out var newUserTgId))
            return;

        if (newUserTgId == -1)
        {
            var removeInvitationsResult = await _invitationService.RemoveGroupInvitationsAsync(groupId, cancellationToken);
            await _botClient.SendMessage(
                chatId,
                removeInvitationsResult.IsSuccess
                    ? "Все заявки отклонены"
                    : "Не удалось отклонить все заявки, попробуйте позже",
                cancellationToken: cancellationToken);
            return;
        }
        
        if (!dialogContext.TryGetData<string>("recalculateAllocationsWithNewUser", out var allocationsStr))
            return;

        var allocations = allocationsStr.Split(' ')
            .Select(decimal.Parse)
            .ToArray();
        if (allocations.Length < 1)
            return;

        if (allocations.Sum() != group.MonthlyReplenishment)
        {
            await _botClient.SendMessage(
                chatId,
                $"Ошибка при распределении бюджета. Сумма всех значений должна равняться {group.MonthlyReplenishment}",
                cancellationToken: cancellationToken);
            return;
        }

        var newUserAllocation = allocations[0];
        var oldUsersAllocations = allocations.Skip(1).ToArray();

        var result = await _groupService.AddUserToGroupAsync(
            groupId,
            newUserTgId,
            Role.Member,
            oldUsersAllocations,
            newUserAllocation,
            SavingStrategy.SaveForNextPeriod);
        await _botClient.SendMessage(
            chatId,
            result.IsSuccess
                ? "Пользователь успешно добавлен в группу"
                : "Не удалось добавить пользователя, попробуйте позже",
            cancellationToken: cancellationToken);
    }

    private async Task HandleRemoveUserAsync(long chatId, DialogContext dialogContext, Guid groupId, CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData<long>("chooseRemoveUser", out var removedUserTgId)
            || !dialogContext.TryGetData<string>("recalculateAllocationsWithDeletedUser", out var allocationsStr))
            return;

        var allocations = allocationsStr.Split(' ')
            .Select(decimal.Parse)
            .ToArray();

        var result = await _groupService.RemoveUserFromGroupAsync(groupId, removedUserTgId, allocations);
        await _botClient.SendMessage(
            chatId,
            result.IsSuccess
                ? "Пользователь успешно удалён из группы"
                : "Не удалось удалить пользователя, попробуйте позже",
            cancellationToken: cancellationToken);
    }
}