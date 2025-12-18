using System.Text;
using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Events;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FinBot.Domain.Models.User;

namespace FinBot.Bll.Implementation.Dialogs;

public class ManageGroupsDialog(
    IGroupService groupService,
    IGenericRepository<User, Guid, PDbContext> userRepository,
    IGenericRepository<Group, Guid, PDbContext> groupRepository,
    IIntegrationsService integrationsService,
    IReportProducer reportProducer,
    ITelegramBotClient botClient,
    IMediator mediator,
    ILogger<ManageGroupsDialog> logger) : IDialogDefinition
{
    public string DialogName { get; } = "ManageGroupsDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0,
            new ChoiceStep<string>(
                "chooseGroup",
                "Выберите группу",
                _ => 1,
                _ => -1,
                ctx =>
                {
                    var buttons = ctx.DialogStorage!["groupsButtons"];

                    return (IEnumerable<(string ButtonName, string ButtonValue)>)buttons;
                },
                async ctx =>
                {
                    var user = await userRepository.GetAll()
                        .Include(u => u.Accounts)
                        .ThenInclude(u => u.Group)
                        .ThenInclude(g => g.Saving)
                        .AsNoTracking()
                        .FirstAsync(u => u.TelegramId == ctx.UserId);

                    var buttons = user.Accounts.Where(a => a.Role == Role.Admin)
                        .Select(a => (a.Group!.Name, a.GroupId.ToString())).ToList();
                    
                    ctx.DialogStorage!["groupsButtons"] = buttons;

                    return Result<IEnumerable<string>>.Success([]);
                },
                isFirstStep: true
            )
        },
        {
            1,
            new ChoiceStep<int>(
                "chooseAction",
                "Что будем делать?\n{{groupBalance}}\n{{accountBalance}}\n{{savingBalance}}",
                ctx => (int)ctx.DialogStorage!["chooseAction"],
                _ => 0,
                ctx =>
                {
                    var savingFlag = (bool)ctx.DialogStorage!["savingFlag"];
                    var savingActiveFlag = (bool)ctx.DialogStorage!["savingActiveFlag"];
                    
                    List<(string ButtonName, int ButtonValue)> buttons = 
                        [
                            ("Перераспределить выплаты", 31),
                            ("Добавить пользователя", 41),
                            ("Удалить пользователя", 51),
                            ("Изменить группу", 61),
                            ("Получить статистику", 71)
                        ];

                    if (savingFlag && !savingActiveFlag)
                    {
                        buttons.Insert(0, ("Цель выполнена! Поставить новую", 21));
                    }
                    else if (savingFlag && savingActiveFlag)
                    {
                        buttons.Insert(0, ("Поставить новую цель", 21));
                    }

                    return buttons;
                },
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Saving)
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);
                    var saving = group.Saving;
                    var account = group.Accounts.SingleOrDefault(a => a.User!.TelegramId == ctx.UserId);
                    
                    ctx.DialogStorage!["savingFlag"] = group.SavingStrategy == SavingStrategy.Save;
                    ctx.DialogStorage!["savingActiveFlag"] = group.Saving!.IsActive;
                    
                    var groupBalance = $"Баланс группы: {Math.Round(group.GroupBalance)} из {Math.Round(group.MonthlyReplenishment)}";
                    var accountBalance = $"Ваш баланс: {Math.Round(account!.Balance)} из {Math.Round(account.DailyAllocation)}";
                    var savingBalance = group.SavingStrategy == SavingStrategy.Save
                        ? $"В копилке {Math.Round(saving.CurrentAmount)} из {Math.Round(saving.TargetAmount)}"
                        : String.Empty;
                    
                    ctx.DialogStorage!["accountBalance"] = accountBalance;
                    ctx.DialogStorage!["groupBalance"] = groupBalance;
                    ctx.DialogStorage!["savingBalance"] = savingBalance;
                    
                    return Result<IEnumerable<string>>.Success(["groupBalance", "accountBalance", "savingBalance"]);
                }
            )
            
        },
        {
            21,
            new TextStep<string>(
                "targetName", 
                "На что хотите накопить?", 
                _ => 22, 
                _ => 1)
        },
        {
            22, 
            new TextStep<decimal>(
                "targetAmount", 
                @"Сколько вам нужно накопить? \(если не целое то через точку\)", 
                _ => -1, 
                _ => 21,
                validate: 
                value => value > 0m
                    ? Result.Success() 
                    : Result.Failure("Введите число больше нуля"))
        },
        {
            31,
            new TextStep<string>(
                "recalculateAllocations",
                "Распределите {{monthlyForRecalculate}} на {{usersString}}\nвведите числа через пробел",
                _ => -1,
                _ => 1,
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Accounts)
                            .ThenInclude(a => a.User)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);

                    var userAccounts = group.Accounts.OrderByDescending(a => a.Id).ToList();
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
                },
                isFirstStep: false,
                validate:
                value =>
                {
                    return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                        ? Result.Success()
                        : Result.Failure("Введите корректные числа");
                }
            )
        },
        {
            41,
            new TextStep<string>(
                "addUserId", 
                "Введите ID нового юзера", 
                _ => 42, 
                _ => 1
               )
        },
        {
            42,
            new ChoiceStep<int>(
                "addUserRole",
                "Какая роль нового юзера?",
                _ => 43,
                _ => 41,
                _ =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Администратор", (int)Role.Admin),
                        ("Участник", (int)Role.Member)
                    ];
                    return buttons;
                })
        },
        {
            43,
            new TextStep<string>(
                "addUserAllocation",
                "Выделите новому пользователю сумму из {{addUserMonthlyForRecalculate}}",
                _ => 44,
                _ => 42,
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Saving)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);

                    ctx.DialogStorage!["addUserMonthlyForRecalculate"] = group.MonthlyReplenishment;
                    
                    return await Task.FromResult(Result<IEnumerable<string>>.Success(["addUserMonthlyForRecalculate"]));
                },
                isFirstStep: false,
                validate:
                value => decimal.TryParse(value , out var num) && num > 0
                    ? Result.Success()
                    : Result.Failure("Введите корректное число"))
        },
        {
            44,
            new TextStep<string>(
                "addUserRecalculateAllocations",
                "Распределите {{leftAfterNewUser}} на {{oldUsersString}} \nвведите числа через пробел",
                _ => 45,
                _ => 43,
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);

                    var userAccounts = group.Accounts.OrderByDescending(a => a.Id).ToList();
                    var accountCount = userAccounts.Count;
                    var usersString = new StringBuilder();

                    for (var i = 0; i < accountCount; i++)
                    {
                        var account = userAccounts[i];
                        usersString.AppendLine($"{i} {account.User!.DisplayName}");
                    }
                    
                    ctx.DialogStorage!["accountCount"] = accountCount;
                    ctx.DialogStorage!["leftAfterNewUser"] = group.MonthlyReplenishment - decimal.Parse((string)ctx.DialogStorage!["addUserAllocation"]);
                    ctx.DialogStorage!["oldUsersString"] = usersString.ToString();

                    return Result<IEnumerable<string>>.Success(["leftAfterNewUser", "accountCount", "oldUsersString"]);
                },
                isFirstStep: false,
                validate:
                value =>
                {
                    return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                        ? Result.Success()
                        : Result.Failure("Введите корректные числа");
                }
            )
        },
        {
            45, 
            new ChoiceStep<int>(
                "addUserStrategy",
                "Что пользователю делать с остатком денег в конце дня?",
                _ => -1,
                _ => 44,
                ctx =>
                {
                    List<(string, int)> buttons =
                    [
                        ("Делим на остаток периода", (int)SavingStrategy.Spread),
                        ("Оставляем на следующий месяц", (int)SavingStrategy.SaveForNextPeriod)
                    ];
                    if (ctx.DialogStorage != null
                        && ctx.DialogStorage.TryGetValue("savingFlag", out var hasTarget)
                        && hasTarget is true)
                        buttons.Add(("Кладем в копилку", (int)SavingStrategy.Save));
                    return buttons;
                }
            )
        },
        {
            51,
            new ChoiceStep<long>(
                "chooseRemoveUser",
                "Выберите пользователя на удаление",
                _ => 52,
                _ => 1,
                ctx =>
                {
                    var buttons = ctx.DialogStorage!["usersButtons"];

                    return (IEnumerable<(string ButtonName, long ButtonValue)>)buttons;
                },
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);

                    var buttons = group.Accounts.Where(a => a.Role != Role.Admin)
                        .Select(a => (a.User!.DisplayName, a.User.TelegramId)).ToList();
                    
                    ctx.DialogStorage!["usersButtons"] = buttons;

                    return Result<IEnumerable<string>>.Success([]);
                }
            )
        },
        {
            52,
            new TextStep<string>(
                "removeUserRecalculateAllocations",
                "Перераспределите {{removeUserMonthlyForRecalculate}} на {{removeUserUsersString}} введите числа через пробел",
                _ => -1,
                _ => 51,
                async ctx =>
                {
                    var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                    var group = await groupRepository.GetAll()
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .AsNoTracking()
                        .FirstAsync(g => g.Id == groupId);

                    var userAccounts = group.Accounts.OrderByDescending(a => a.Id).ToList();
                    var accountCount = userAccounts.Count;
                    var usersString = new StringBuilder();

                    for (var i = 0; i < accountCount; i++)
                    {
                        var account = userAccounts[i];
                        usersString.AppendLine($"{i} {account.User!.DisplayName}");
                    }
                    
                    ctx.DialogStorage!["removeUserMonthlyForRecalculate"] = group.MonthlyReplenishment;
                    ctx.DialogStorage!["removeUserUsersString"] = usersString.ToString();

                    return Result<IEnumerable<string>>.Success(["monthlyForRecalculate", "usersString", "removeUserMonthlyForRecalculate", "removeUserUsersString"]);
                },
                isFirstStep: false,
                validate:
                value =>
                {
                    return value.Split(' ').All(x => decimal.TryParse(x, out var num) && num > 0)
                        ? Result.Success()
                        : Result.Failure("Введите корректные числа");
                }
            )
        },
        {
            61, 
            new TextStep<string>(
                "newGroupName", 
                "Введите название группы", 
                _ => 62, 
                _ => 1
                )
        },
        {
            62, 
            new TextStep<decimal>(
                "newReplenishment", 
                @"Введите пополнение группы \(если не целое то через точку\)", 
                _ => 63, 
                _ => 62,
                validate: 
                value => value > 0m
                    ? Result.Success() 
                    : Result.Failure("Введите число больше нуля"))
        },
        {
            63, 
            new ChoiceStep<int>(
                "newDebtStrategy",
                "Что делать с долгами если не рассчитали расходы?",
                _ => 64,
                _ => 62,
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
        },
        {
            64, 
            new ChoiceStep<int>(
                "newSavingStrategy",
                "Что делать с остатком денег в конце месяца?",
                _ => -1,
                _ => 63,
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
        },
        {
            71,
            new ChoiceStep<bool>(
                "GetGroupStatistic",
                "Чью статистику смотрим?",
                _ => 72,
                _ => 1,
                _ =>
                {
                    List<(string, bool)> buttons =
                    [
                        ("Мою", false),
                        ("Группы", true)
                    ];
                    return buttons;
                })
        }
        ,
        {
            72,
            new ChoiceStep<int>(
                "ChooseReportType",
                "Какой отчёт вы желаете получить?",
                _ => 73,
                _ => 71,
                _ =>
                {
                    var buttons = new List<(string, int)>()
                    {
                        ("Таблица", (int)ReportType.ExcelTable),
                        ("Гистограмма", (int)ReportType.CategoryChart),
                        ("Линейный график", (int)ReportType.LineChart)
                    };
                    
                    return buttons;
                }
            )
            
        },
        {
         73,
         new ChoiceStep<string>(
             "Temp",
             "Отчет загружается",
             _ => -1,
             _ => 72,
             _ =>
             {
                 return new List<(string, string)>()
                 {
                     ("Получить", "done")
                 };
             },
             async ctx =>
             {
                 var isGroupStatistic = (bool)ctx.DialogStorage!["GetGroupStatistic"];
                 var chooseReportType = (ReportType)(int)ctx.DialogStorage!["ChooseReportType"];
                 var groupId = Guid.Parse((string)ctx.DialogStorage!["chooseGroup"]);
                 var userId = (await userRepository.FirstOrDefaultAsync(u => u.TelegramId == ctx.UserId))!.Id;
                 ReportGenerationEvent evt;

                 switch (chooseReportType)
                 {
                     case ReportType.ExcelTable:
                         evt = new ReportGenerationEvent 
                         { 
                             GroupId = groupId, 
                             UserId = isGroupStatistic ? null : userId, 
                             Type = ReportType.ExcelTable 
                         };
    
                         var result = await reportProducer.QueueReportGenerationAsync(evt);
                         break;
                     
                     case ReportType.CategoryChart:
                         evt = new ReportGenerationEvent 
                         { 
                             GroupId = groupId, 
                             UserId = isGroupStatistic ? null : userId, 
                             Type = ReportType.CategoryChart 
                         };
                         await reportProducer.QueueReportGenerationAsync(evt);
                         break;
                     
                     case ReportType.LineChart:
                         evt = new ReportGenerationEvent 
                         { 
                             GroupId = groupId, 
                             UserId = isGroupStatistic ? null : userId, 
                             Type = ReportType.LineChart 
                         };
        
                         await reportProducer.QueueReportGenerationAsync(evt);
                         break;
                     
                     default:
                         throw new ArgumentOutOfRangeException();
                 }

                 return Result<IEnumerable<string>>.Success([]);
             })
        }
    };

    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update,
        CancellationToken cancellationToken)
    {
        var completedOperation = (long)dialogContext.DialogStorage!["chooseAction"];
        var groupId = Guid.Parse((string)dialogContext.DialogStorage!["chooseGroup"]);
        var group = await groupRepository.GetAll()
            .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        var user = await userRepository.FirstOrDefaultAsync(u => u.TelegramId == dialogContext.UserId);
        
        if (group == null)
        {
            return;
        }
        
        switch (completedOperation)
        {
            case 21:
                var targetName = (string)dialogContext.DialogStorage!["targetName"];
                var targetCost = (decimal)dialogContext.DialogStorage!["targetAmount"];

                var changeGoalResult = await groupService.ChangeGoalAsync(group, targetName, targetCost);
                
                if (!changeGoalResult.IsSuccess)
                {
                    return;
                }
                await botClient.SendMessage(
                    chatId,
                    "Цель успешно изменена",
                    parseMode: ParseMode.MarkdownV2, 
                    cancellationToken: cancellationToken);
                await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
                break;
            
            case 31:
                var recalculateAllocationsString = (string)dialogContext.DialogStorage!["recalculateAllocations"];
                var recalculateAllocations = recalculateAllocationsString.Split(' ').Select(x => decimal.Parse(x)).ToArray();

                if (group.Accounts.Count != recalculateAllocations.Length)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Введено недостаточно значений",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    return;
                }

                var recalculateResult = await groupService.RecalculateMonthlyAllocationsAsync(group, recalculateAllocations);
                if (!recalculateResult.IsSuccess)
                {
                    return;
                }
                
                await botClient.SendMessage(
                    chatId,
                    "Распределение успешно изменено",
                    parseMode: ParseMode.MarkdownV2, 
                    cancellationToken: cancellationToken);
                await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
                break;
            
            case 41:
                var newUserId = Guid.Parse((string)dialogContext.DialogStorage!["addUserId"]);
                var newUserRole = (Role)(long)dialogContext.DialogStorage!["addUserRole"];
                var addUserAllocation = decimal.Parse((string)dialogContext.DialogStorage!["addUserAllocation"]);
                var addUserOldAllocations = ((string)dialogContext.DialogStorage!["addUserRecalculateAllocations"]).Split(' ').Select(x => decimal.Parse(x)).ToArray();
                var addUserStrategy = (SavingStrategy)(int)dialogContext.DialogStorage!["addUserStrategy"];
                
                if (group.Accounts.Count != addUserOldAllocations.Length)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Введено недостаточно значений для уже существующих пользователей",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    return;
                }

                var addUserResult = await groupService.AddUserToGroupAsync(group, newUserId, newUserRole,
                    addUserOldAllocations, addUserAllocation, addUserStrategy);
                if (!addUserResult.IsSuccess)
                {
                    await botClient.SendMessage(
                        chatId,
                        $"Произошла ошибка: {addUserResult.ErrorMessage}",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    return;
                }
                
                await botClient.SendMessage(
                    chatId,
                    "Пользователь успешно добавлен",
                    parseMode: ParseMode.MarkdownV2, 
                    cancellationToken: cancellationToken);
                await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
                break;

            case 51:
                var userToDeleteId = (long)dialogContext.DialogStorage!["chooseRemoveUser"];
                var removeUserRecalculateAllocations = ((string)dialogContext.DialogStorage!["removeUserRecalculateAllocations"]).Split(' ').Select(x => decimal.Parse(x)).ToArray();
                if (group.Accounts.Count != removeUserRecalculateAllocations.Length + 1)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Введено недостаточно значений для оставшихся пользователей",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    return;
                }
                
                var removeUserResult = await groupService.RemoveUserFromGroupAsync(group, userToDeleteId, removeUserRecalculateAllocations);
                if (!removeUserResult.IsSuccess)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Произошла ошибка",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                }
                await botClient.SendMessage(
                    chatId,
                    "Пользователь успешно удалён",
                    parseMode: ParseMode.MarkdownV2, 
                    cancellationToken: cancellationToken);
                await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
                break;
            
            case 61:
                try
                {
                    var newGroupName = (string)dialogContext.DialogStorage!["newGroupName"];
                    var newReplenishment = decimal.Parse((string)dialogContext.DialogStorage!["newReplenishment"]);
                    var newDebtStrategy = (DebtStrategy)(long)dialogContext.DialogStorage!["newDebtStrategy"];
                    var newSavingStrategy = (SavingStrategy)(long)dialogContext.DialogStorage!["newSavingStrategy"];

                    group.Name = newGroupName;
                    group.MonthlyReplenishment = newReplenishment;
                    group.SavingStrategy = newSavingStrategy;
                    group.DebtStrategy = newDebtStrategy;

                    groupRepository.Update(group);
                    await groupRepository.SaveChangesAsync();
                    
                    await botClient.SendMessage(
                        chatId,
                        "Группа успешно изменена",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
                    break;
                }
                catch (Exception)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Произошла ошибка",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    break;
                }
                
            case 71:
                var isGroupStatistic = (bool)dialogContext.DialogStorage!["GetGroupStatistic"];
                var chooseReportType = (ReportType)(long)dialogContext.DialogStorage!["ChooseReportType"];
                Result<byte[]> reportResult;

                switch (chooseReportType)
                {
                    case ReportType.ExcelTable:
                        reportResult = isGroupStatistic 
                            ? await integrationsService.GetExcelTableForGroup(groupId) 
                            : await integrationsService.GetExcelTableForUserInGroup(user!.Id, groupId);
                        break;
                    case ReportType.CategoryChart:
                        reportResult = isGroupStatistic 
                            ? await integrationsService.GetDiagramForGroup(groupId) 
                            : await integrationsService.GetDiagramForUserInGroup(user!.Id, groupId);
                        break;
                    case ReportType.LineChart:
                        reportResult = isGroupStatistic 
                            ? await integrationsService.GetLineChartForGroup(groupId) 
                            : await integrationsService.GetLineChartForUserInGroup(user!.Id, groupId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                if (!reportResult.IsSuccess)
                {
                    logger.LogError("Error while getting report: {error}", reportResult.ErrorMessage);
                    await botClient.SendMessage(
                        chatId,
                        "Произошла ошибка",
                        parseMode: ParseMode.MarkdownV2, 
                        cancellationToken: cancellationToken);
                    
                    break;
                }
                
                byte[] report = reportResult.Data;

                using (var stream = new MemoryStream(report))
                {
                    var filename = chooseReportType == ReportType.ExcelTable ? "report.xlsx" : "report.png";
                    await botClient.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(stream, fileName: filename),
                        cancellationToken: cancellationToken
                    );
                }
                break;
            
            default:
                logger.LogCritical("-1111");
                break;
        }

    }
}