using FinBot.Domain.Models;

namespace FinBot.Llm.Repositories;

public interface IExpenseRepository
{
    Task<List<Expense>> GetExpensesForUserInGroupAsync(long userId, Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}