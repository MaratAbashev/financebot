using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class GroupBackgroundService(
    PDbContext dbContext,
    ILogger<GroupBackgroundService> logger) : IGroupBackgroundService
{
    public async Task<Result> MonthlyGroupRefreshAsync(Guid groupId)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var group = await dbContext.Groups
                .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
                .Include(g => g.Saving)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                return Result.Failure($"Group with id {groupId} not found for monthly recalculation", ErrorType.NotFound);
            }

            var saving = group.Saving!;
            var accounts = group.Accounts;
            var replenishment = group.MonthlyReplenishment;

            ApplySavingStrategyToAccounts(group);

            if (group.GroupBalance < 0)
            {
                switch (group.DebtStrategy)
                {
                    case DebtStrategy.Nullify:
                        group.GroupBalance = 0;
                        break;

                    case DebtStrategy.FromSaving:
                        if (saving.CurrentAmount > decimal.Abs(group.GroupBalance))
                        {
                            saving.CurrentAmount += group.GroupBalance;
                            group.GroupBalance = 0;
                        }
                        else
                        {
                            group.GroupBalance += saving.CurrentAmount;
                            saving.CurrentAmount = 0;
                            replenishment += group.GroupBalance;
                        }

                        break;

                    case DebtStrategy.FromNextMonth:
                        if (replenishment > decimal.Abs(group.GroupBalance))
                        {
                            replenishment += group.GroupBalance;
                            group.GroupBalance = 0;
                        }
                        else
                        {
                            group.GroupBalance += replenishment;
                            replenishment = 0;
                        }

                        break;
                }
            }

            switch (group.SavingStrategy)
            {
                case SavingStrategy.Spread:
                    replenishment += group.GroupBalance;
                    break;

                case SavingStrategy.Save:
                    saving.CurrentAmount = group.GroupBalance < 0
                        ? saving.CurrentAmount
                        : saving.CurrentAmount + group.GroupBalance;
                    break;
            }

            group.GroupBalance = replenishment;

            var monthlySum = accounts.Sum(a => a.MonthlyAllocation);
            var weight = monthlySum == 0 ? 0m : group.GroupBalance / monthlySum;
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            foreach (var account in group.Accounts)
            {
                account.MonthlyAllocation = Math.Round(account.MonthlyAllocation * weight, 2, MidpointRounding.ToZero);
                account.DailyAllocation = Math.Round(account.MonthlyAllocation / daysInMonth, 2, MidpointRounding.ToZero);
                account.Balance += account.DailyAllocation;
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex,
                "Something went wrong during monthly group refresh: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result.Failure("Failed to refresh group monthly");
        }
    }

    public async Task<Result> DailyAccountsRecalculateAsync(Guid groupId)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var group = await dbContext.Groups
                .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
                .Include(g => g.Saving)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                return Result.Failure($"Group with id {groupId} not found for daily recalculation", ErrorType.NotFound);
            }

            var accounts = group.Accounts;

            ApplySavingStrategyToAccounts(group);

            var monthlySum = accounts.Sum(a => a.MonthlyAllocation);
            var weight = monthlySum == 0 ? 0m : group.GroupBalance / monthlySum;
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            var daysLeft = daysInMonth - (DateTime.Now.Day - 1);
            foreach (var account in accounts)
            {
                account.MonthlyAllocation = Math.Round(account.MonthlyAllocation * weight, 2, MidpointRounding.ToZero);
                account.DailyAllocation = Math.Round(account.MonthlyAllocation / daysLeft, 2, MidpointRounding.ToZero);
                account.Balance += account.DailyAllocation;
                group.GroupBalance -= account.DailyAllocation;
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(
                ex,
                "Something went wrong during daily accounts recalculation: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result.Failure("Failed to recalculate accounts daily");
        }
    }

    private static void ApplySavingStrategyToAccounts(Group group)
    {
        var saving = group.Saving!;

        foreach (var account in group.Accounts)
        {
            if (account.Balance < 0)
            {
                group.GroupBalance += account.Balance;
                account.Balance = 0;
            }

            switch (account.SavingStrategy)
            {
                case SavingStrategy.Spread:
                    group.GroupBalance += account.Balance;
                    account.Balance = 0;
                    break;

                case SavingStrategy.Save:
                    saving.CurrentAmount += account.Balance;
                    account.Balance = 0;
                    break;

                case SavingStrategy.SaveForNextPeriod:
                    break;
            }
        }

        if (saving.CurrentAmount >= saving.TargetAmount)
        {
            saving.IsActive = false;
        }
    }
}