using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class ExpenseService(
    PDbContext dbContext,
    ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<Result<decimal>> AddExpenseAsync(
        long userTgId,
        Guid groupId,
        decimal amount,
        ExpenseCategory category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetUserWithAccountsByTgIdAsync(userTgId, cancellationToken);
            if (user is null)
            {
                logger.LogError("User {userTgId} does not exist", userTgId);
                return Result<decimal>.Failure("User does not exist", ErrorType.NotFound);
            }

            var groupExists = await dbContext.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
            if (!groupExists)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<decimal>.Failure("Group does not exist", ErrorType.NotFound);
            }

            var account = user.Accounts.FirstOrDefault(a => a.GroupId == groupId);
            if (account is null)
            {
                logger.LogError("User {userTgId} does not have an Account in group {groupId}", userTgId, groupId);
                return Result<decimal>.Failure("Account not found in group", ErrorType.NotFound);
            }

            var newExpense = new Expense
            {
                Category = category,
                Amount = amount,
                Date = DateTime.UtcNow,
                UserId = user.Id,
                GroupId = groupId
            };

            account.Balance -= amount;
            dbContext.Expenses.Add(newExpense);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<decimal>.Success(account.Balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during add expense: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<decimal>.Failure("Failed to add expense");
        }
    }

    public async Task<Result<IEnumerable<Expense>>> GetPendingExpensesAsync(
        long userTgId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expenses = await dbContext.Expenses
                .Where(e => e.User!.TelegramId == userTgId && e.GroupId == null)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<Expense>>.Success(expenses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during get pending expenses: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<IEnumerable<Expense>>.Failure("Failed to get pending expenses");
        }
    }

    public async Task<Result> DistributeExpensesAsync(
        long userTgId, int expenseId, Guid? groupId, ExpenseCategory? category,
        bool reject = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await dbContext.Expenses
                .FirstOrDefaultAsync(e => e.Id == expenseId);
            if (expense is null)
            {
                logger.LogError("Expense {expenseId} does not exist", expenseId);
                return Result.Failure("Expense does not exist", ErrorType.NotFound);
            }

            if (reject)
            {
                dbContext.Expenses.Remove(expense);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            var user = await GetUserWithAccountsByTgIdAsync(userTgId, cancellationToken);
            if (user is null)
            {
                logger.LogError("User with tgId {userTgId} does not exist", userTgId);
                return Result.Failure("User does not exist", ErrorType.NotFound);
            }

            var groupExists = await dbContext.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
            if (!groupExists)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result.Failure("Group does not exist", ErrorType.NotFound);
            }

            var account = user.Accounts.FirstOrDefault(a => a.GroupId == groupId);
            if (account is null)
            {
                logger.LogError("User {userTgId} does not have an Account in group {groupId}", userTgId, groupId);
                return Result.Failure("Account not found in group", ErrorType.NotFound);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                expense.GroupId = groupId;
                expense.Category = category ?? ExpenseCategory.Other;
                account.Balance -= expense.Amount;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to distribute expense {expenseId}: {errorMessage}", expenseId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure("Failed to distribute expense");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to distribute expense {expenseId}: {errorMessage}", expenseId, ex.Message);
            return Result.Failure("Failed to distribute expense");
        }

        return Result.Success();
    }

    private Task<User?> GetUserWithAccountsByTgIdAsync(long tgId, CancellationToken ct) =>
        dbContext.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.TelegramId == tgId, ct);
}