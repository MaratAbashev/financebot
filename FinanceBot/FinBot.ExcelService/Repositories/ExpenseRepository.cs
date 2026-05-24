using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;

namespace FinBot.ExcelService.Repositories;

public class ExpenseRepository(ReadDbContext dbContext) : IExpenseRepository
{
    public async Task<Result<List<Expense>>> GetForUserInGroupAsync(long userTgId, Guid groupId,
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.TelegramId == userTgId, cancellationToken);
        if (!userExists)
            return Result<List<Expense>>.Failure($"User with tgId {userTgId} not found", ErrorType.NotFound);

        var groupExists = await dbContext.Groups.AsNoTracking()
            .AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            return Result<List<Expense>>.Failure($"Group with id {groupId} not found", ErrorType.NotFound);

        var accountExists = await dbContext.Accounts.AsNoTracking()
            .AnyAsync(a => a.User!.TelegramId == userTgId && a.GroupId == groupId, cancellationToken);
        if (!accountExists)
            return Result<List<Expense>>.Failure(
                $"User with tgId {userTgId} is not a member of group with id {groupId}", ErrorType.NotFound);

        var expenses = await dbContext.Expenses.AsNoTracking()
            .Where(e => e.User!.TelegramId == userTgId && e.GroupId == groupId && e.Date >= from && e.Date < to)
            .ToListAsync(cancellationToken);

        return Result<List<Expense>>.Success(expenses);
    }

    public async Task<Result<List<Expense>>> GetForGroupAsync(Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        var groupExists = await dbContext.Groups.AsNoTracking()
            .AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            return Result<List<Expense>>.Failure($"Group with id {groupId} not found", ErrorType.NotFound);

        var expenses = await dbContext.Expenses.AsNoTracking()
            .Include(e => e.User)
            .Where(e => e.GroupId == groupId && e.Date >= from && e.Date < to)
            .ToListAsync(cancellationToken);

        return Result<List<Expense>>.Success(expenses);
    }
}