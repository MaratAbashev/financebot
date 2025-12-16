using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Models.SavingModel;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class GroupBackgroundService(IUnitOfWork<PDbContext> unitOfWork, ILogger<IGroupBackgroundService> logger) : IGroupBackgroundService
{
    public async Task<Result> MonthlyGroupRefreshAsync(Group group)
    {
        await using var transaction = unitOfWork.BeginDbTransaction();
        try
        {
            var saving = group.Saving!;
            var accounts = group.Accounts;
            var replenishment = group.MonthlyReplenishment;

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

            // todo: Проверка на достижение цели

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
                            saving.CurrentAmount -= group.GroupBalance;
                            group.GroupBalance = 0;
                        }
                        else
                        {
                            group.GroupBalance -= saving.CurrentAmount;
                            saving.CurrentAmount = 0;
                            replenishment -= group.GroupBalance;
                        }

                        break;

                    case DebtStrategy.FromNextMonth:
                        if (replenishment > decimal.Abs(group.GroupBalance))
                        {
                            replenishment -= group.GroupBalance;
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

            var weight = group.GroupBalance / accounts.Select(a => a.MonthlyAllocation).Sum();
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            foreach (var account in group.Accounts)
            {
                account.MonthlyAllocation *= weight;
                account.DailyAllocation = Math.Round(account.MonthlyAllocation / daysInMonth, 2, MidpointRounding.ToZero);
                account.Balance += account.DailyAllocation;
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError("Something went wrong during monthly group refresh: {errorMessage}", ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DailyAccountsRecalculateAsync(Group group)
    {
        await using var transaction = unitOfWork.BeginDbTransaction();
        try
        {
            var saving = group.Saving!;
            var accounts = group.Accounts;

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

            // todo: Проверка на достижение цели

            var weight = group.GroupBalance / accounts.Select(a => a.MonthlyAllocation).Sum();
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            foreach (var account in group.Accounts)
            {
                account.MonthlyAllocation *= weight;
                account.DailyAllocation = Math.Round(account.MonthlyAllocation / daysInMonth, 2, MidpointRounding.ToZero);
                account.Balance += account.DailyAllocation;
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError("Something went wrong during daily accounts recalculation: {errorMessage}", ex.Message);
            return Result.Failure(ex.Message);
        }
    }
}