using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FinBot.Domain.Models.User;

namespace FinBot.Bll.Implementation.Dialogs;

public class AddExpenseDialog(IGenericRepository<User, Guid, PDbContext> userRepository,
    IMediator mediator,
    IUserService userService,
    ITelegramBotClient botClient,
    IGenericRepository<Account, int, PDbContext> accountRepository): IDialogDefinition
{
    public string DialogName => "AddExpenseDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new ChoiceStep<string>(
                "groupId", 
                "Выберите группу", 
                _ => 1, 
                _ => -1,
                ctx=>
                {
                    var buttons = ctx.DialogStorage!["buttons"];

                    return (IEnumerable<(string ButtonName, string ButtonValue)>)buttons;
                },
                async ctx =>
                {
                    var user = await userRepository.GetAll()
                        .Include(u => u.Accounts)
                        .ThenInclude(u => u.Group)
                        .FirstOrDefaultAsync(u => u.TelegramId == ctx.UserId);
                    if (user == null)
                        return Result<IEnumerable<string>>.Failure("User not found");
                    if (user.Accounts.Count == 0)
                        return Result<IEnumerable<string>>.Failure("No accounts found");
                    var buttons = user.Accounts.Select(a => (a.Group!.Name, a.GroupId.ToString())).ToList();
                    ctx.DialogStorage!["buttons"] = buttons;
                    
                    return Result<IEnumerable<string>>.Success([]);
                },
                onPromptFailed: async (_, chatId, update, _) =>
                {
                    await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId));
                },
                isFirstStep: true)
        },
        {
            1, 
            new TextStep<decimal>(
                "expense", 
                "Вам доступно {{amount}} руб\nВведите трату \\(если не целое то через точку\\)"
                    .Replace(".", "\\."), 
                _ => 2, 
                _ => 0,
                dataLoader: async ctx =>
                {
                    if (!Guid.TryParse(ctx.DialogStorage!["groupId"].ToString(), out var groupId))
                        return Result<IEnumerable<string>>.Failure("Cant parse Guid");
                    var account = await accountRepository.FindBy(a => a.GroupId == groupId)
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.User!.TelegramId == ctx.UserId);
                    if (account == null)
                        return Result<IEnumerable<string>>.Failure("No accounts found");
                    ctx.DialogStorage!["amount"] = (int)account.Balance;
                    return Result<IEnumerable<string>>.Success(["amount"]);
                },
                validate: 
                value => value > 0m
                    ? Result.Success() 
                    : Result.Failure("Введите число больше нуля"))
        },
        {
            2, 
            new ChoiceStep<int>(
                "expenseCategory", 
                "Выберите категорию траты", 
                _ => -1, 
                _ => 1,
                ctx=> [
                    ("Еда", (int)ExpenseCategory.Food),
                    ("Транспорт", (int)ExpenseCategory.Transport),
                    ("Коммунальные услуги", (int)ExpenseCategory.Housing),
                    ("Шоппинг", (int)ExpenseCategory.Shopping),
                    ("Развлечения", (int)ExpenseCategory.Entertainment),
                    ("Здоровье", (int)ExpenseCategory.Health),
                    ("Другое", (int)ExpenseCategory.Other),
                ])
        },
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindBy(u => u.TelegramId == chatId)
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (user == null)
            return;
        if (!TryGetData<string>(dialogContext, "groupId", out var groupId)
            || !TryGetData<decimal>(dialogContext, "expense", out var expense)
            || !TryGetData<int>(dialogContext, "expenseCategory", out var expenseCategory)
            || !Guid.TryParse(groupId, out var groupIdGuid))
            return;
        //TODO починить добавление
        var addExpenseResult = await userService.AddExpenseAsync(user, groupIdGuid, expense, (ExpenseCategory)expenseCategory);
        if (!addExpenseResult.IsSuccess)
        {
            await botClient.SendMessage(
                chatId, 
                "Не удалось добавить выплату, попробуйте еще раз", 
                parseMode: ParseMode.MarkdownV2, 
                cancellationToken: cancellationToken);
            await mediator.Send(new StartDialogRequest(update, "MenuDialog", chatId), cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId,
            $"Успешно, ваш остаток на сегодня: {addExpenseResult.Data} руб", 
            cancellationToken: cancellationToken);
    }
    
    private bool TryGetData<T>(DialogContext dialogContext, string key, out T data) where T : IConvertible
    {
        data = default!;  // всегда инициализируем
    
        if (dialogContext.DialogStorage == null
            || !dialogContext.DialogStorage.TryGetValue(key, out var boxedData))
        {
            return false;  // нет данных
        }

        try
        {
            // pattern matching + Convert.ChangeType
            if (boxedData is T directData)
            {
                data = directData;
                return true;
            }
        
            data = (T)Convert.ChangeType(boxedData, typeof(T));
            return true;
        }
        catch
        {
            return false;  // конверсия не удалась
        }
    }
}