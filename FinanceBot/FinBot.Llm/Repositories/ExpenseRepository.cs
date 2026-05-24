using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.Llm.Repositories;

public class ExpenseRepository(ReadDbContext dbContext) : IExpenseRepository
{
    public async Task<List<Expense>> GetExpensesForUserInGroupAsync(long userId, Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.TelegramId == userId, cancellationToken);
        if (!userExists)
            throw new ArgumentException($"User with id {userId} not found", nameof(userId));

        var groupExists = await dbContext.Groups.AsNoTracking()
            .AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            throw new ArgumentException($"Group with id {groupId} not found", nameof(groupId));

        var accountExists = await dbContext.Accounts
            .Include(a => a.User)
            .AsNoTracking()
            .AnyAsync(a => a.User != null && a.User.TelegramId == userId && a.GroupId == groupId, cancellationToken);
        if (!accountExists)
            throw new ArgumentException($"User with id {userId} is not a member of group with id {groupId}",
                nameof(userId));

        var expenses = await dbContext.Expenses
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.User != null && e.User.TelegramId == userId && e.GroupId == groupId && e.Date >= from && e.Date < to)
            .ToListAsync(cancellationToken);

        return expenses;
    }
}