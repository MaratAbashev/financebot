using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IExpenseService
{
    Task<Result<decimal>> AddExpenseAsync(
        long userTgId,
        Guid groupId,
        decimal amount,
        ExpenseCategory category,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<Expense>>> GetPendingExpensesAsync(
        long userTgId,
        CancellationToken cancellationToken = default);

    Task<Result> DistributeExpensesAsync(
        long userTgId,
        int expenseId, Guid? groupId, ExpenseCategory? category, bool reject = false,
        CancellationToken cancellationToken = default);
}