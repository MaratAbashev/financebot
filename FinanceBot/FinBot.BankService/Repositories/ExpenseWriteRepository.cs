using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;

namespace FinBot.BankService.Repositories;

public class ExpenseWriteRepository(PDbContext context) : IExpenseWriteRepository
{
    public async Task AddAsync(Guid userId, decimal amount, ExpenseCategory category, DateTime date,
        CancellationToken ct = default)
    {
        context.Expenses.Add(new Expense
        {
            UserId = userId,
            GroupId = null,
            Amount = amount,
            Category = category,
            Date = date
        });

        await context.SaveChangesAsync(ct);
    }
}