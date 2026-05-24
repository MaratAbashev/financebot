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

public class ConfigureGroupDialog : IDialogDefinition
{
    private readonly IMediator _mediator;
    private readonly IGroupService _groupService;
    private readonly ITelegramBotClient _botClient;

    public ConfigureGroupDialog(IMediator mediator,
        IGroupService groupService,
        ITelegramBotClient botClient)
    {
        _mediator = mediator;
        _groupService = groupService;
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

        builder.AddTextStep<string>(
                "newGroupName",
                "Введите новое название группы"
            )
            .WithValidation(value => value.Length is > 0 and <= 20
                ? Result.Success()
                : Result.Failure("Длина названия должна быть от 1 до 20 символов"))
            .Commit();

        builder.AddTextStep<decimal>(
                "newReplenishment",
                @"Введите пополнение группы (если не целое то через точку)")
            .WithValidation(
                value => value > 0m
                    ? Result.Success()
                    : Result.Failure("Введите число больше нуля"))
            .Commit();

        builder.AddChoiceStep(
                "newDebtStrategy",
                "Что делать с долгами если не рассчитали расходы?",
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Прощаем", (int)DebtStrategy.Nullify),
                        ("Берем с пополнения следующего месяца", (int)DebtStrategy.FromNextMonth)
                    ];
                    if (ctx.DialogStorage != null
                        && ctx.DialogStorage.TryGetValue("savingFlag", out var hasTarget)
                        && hasTarget is true)
                        buttons.Add(("Берем с копилки", (int)DebtStrategy.FromSaving));
                    return buttons;
                })
            .Commit();

        builder.AddChoiceStep(
            "newSavingStrategy",
            "Что делать с остатком денег в конце месяца?",
            ctx =>
            {
                List<(string, int)> buttons =
                [
                    ("Оставляем на следующий месяц", (int)SavingStrategy.SaveForNextPeriod)
                ];
                if (ctx.DialogStorage != null
                    && ctx.DialogStorage.TryGetValue("savingFlag", out var hasTarget)
                    && hasTarget is true)
                    buttons.Add(("Кладем в копилку", (int)SavingStrategy.Save));
                return buttons;
            })
            .Commit();

        builder.AddTextStep<string>(
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

                if (!ctx.TryGetData<decimal>("newReplenishment", out var newReplenishment))
                {
                    return Result<IEnumerable<string>>.Failure("Ошибка в новом пополнении");
                }
                
                ctx.DialogStorage!["accountCount"] = accountCount;
                ctx.DialogStorage!["monthlyForRecalculate"] = newReplenishment;
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

        return builder.Build();
    }
    public string DialogName => nameof(ConfigureGroupDialog);
    public IReadOnlyDictionary<int, IStep> Steps { get; }
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        if (!dialogContext.TryGetData<string>("chooseGroup", out var groupIdStr)
            || !Guid.TryParse(groupIdStr, out var groupId)
            || !dialogContext.TryGetData<string>("newGroupName", out var newGroupName)
            || !dialogContext.TryGetData<decimal>("newReplenishment", out var newReplenishment)
            || !dialogContext.TryGetData<int>("newDebtStrategy", out var newDebtStrategy)
            || !dialogContext.TryGetData<int>("newSavingStrategy", out var newSavingStrategy)
            || !dialogContext.TryGetData<string>("recalculateAllocations", out var allocationsStr))
            return;

        var allocations = allocationsStr.Split(' ')
            .Select(decimal.Parse)
            .ToArray();

        if (allocations.Sum() != newReplenishment)
        {
            await _botClient.SendMessage(
                chatId,
                $"Ошибка при распределении бюджета. Сумма всех значений должна равняться {newReplenishment}",
                cancellationToken: cancellationToken);
            await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
            return;
        }

        var updateResult = await _groupService.UpdateGroupAsync(
            groupId,
            newGroupName,
            newReplenishment,
            (SavingStrategy)newSavingStrategy,
            (DebtStrategy)newDebtStrategy);
        if (!updateResult.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                "Не удалось обновить настройки группы, попробуйте еще раз",
                cancellationToken: cancellationToken);
            await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
            return;
        }

        var recalculateResult = await _groupService.RecalculateMonthlyAllocationsAsync(groupId, allocations);

        await _botClient.SendMessage(
            chatId,
            recalculateResult.IsSuccess
                ? "Настройки группы успешно обновлены"
                : "Не удалось пересчитать распределение, попробуйте позже",
            cancellationToken: cancellationToken);

        await _mediator.Send(new StartDialogRequest(update, nameof(MenuDialog), chatId), cancellationToken);
    }
}