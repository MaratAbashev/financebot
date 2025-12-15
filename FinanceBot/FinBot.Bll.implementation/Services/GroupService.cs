using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Models.SavingModel;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class GroupService(
    IGenericRepository<Group, Guid, PDbContext> groupRepository,
    IGenericRepository<Saving, Guid, PDbContext> savingRepository,
    IUnitOfWork<PDbContext> unitOfWork,
    IUserService userService,
    ILogger<GroupService> logger) : IGroupService
{
    public async Task<Result<Group>> CreateGroupAsync(
        string groupName,
        User creator,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount)
    {
        try
        {
            var now = DateTime.Now;
            var daysInMonthLeft = DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);
            var dailyUserAllocation = replenishment / daysInMonthLeft;
            var todayGroupBalance = replenishment - dailyUserAllocation;

            var newGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = groupName,
                GroupBalance = todayGroupBalance,
                MonthlyReplenishment = replenishment,
                SavingStrategy = groupSavingStrategy,
                DebtStrategy = debtStrategy,
                Accounts =
                [
                ],
                CreatorId = creator.Id,
                Creator = creator,
                Saving = null
            };

            var newSaving = new Saving
            {
                Id = Guid.NewGuid(),
                Name = savingTargetName,
                TargetAmount = savingTargetAmount ?? -1,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = DateTime.Now,
                GroupId = newGroup.Id,
                Group = newGroup
            };

            var newAccount = new Account
            {
                Role = Role.Admin,
                DailyAllocation = dailyUserAllocation,
                SavingStrategy = accountSavingStrategy,
                Balance = dailyUserAllocation,
                UserId = creator.Id,
                User = creator,
                GroupId = newGroup.Id,
                Group = newGroup
            };

            newGroup.Saving = newSaving;
            newGroup.Accounts.Add(newAccount);

            creator.Groups.Add(newGroup);
            creator.Accounts.Add(newAccount);

            await groupRepository.AddAsync(newGroup);
            await groupRepository.SaveChangesAsync();

            return Result<Group>.Success(newGroup);
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during create group: {errorMessage}", ex.Message);
            return Result<Group>.Failure(ex.Message);
        }
    }

    public async Task<Result> RecalculateAllocationsAsync(Group group, decimal[] allocations)
    {
        try
        {
            var accounts = group.Accounts.OrderBy(a => a.Id).ToList();
            for (var i = 0; i < accounts.Count; i++)
            {
                accounts[i].DailyAllocation = allocations[i];
            }

            await unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during recalculate allocations: {errorMessage}", ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<Saving>> ChangeGoalAsync(Group group, string savingTargetName, int savingTargetAmount)
    {
        try
        {
            var saving = group.Saving!;
            saving.Name = savingTargetName;
            saving.TargetAmount = savingTargetAmount;
            saving.CurrentAmount = 0;
            saving.CreatedAt = DateTime.Now;

            savingRepository.Update(saving);
            await savingRepository.SaveChangesAsync();

            return Result<Saving>.Success(saving);
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during change goal: {errorMessage}", ex.Message);
            return Result<Saving>.Failure(ex.Message);
        }
    }

    public async Task<Result<Account>> AddUserAsyncToGroup(
        Group group,
        Guid newUserId,
        long newUserTgId,
        string newUserDisplayName,
        Role newUserRole,
        decimal[] oldUserAllocations,
        decimal newUserAllocation, SavingStrategy newUserSavingStrategy)
    {
        await using var transaction = unitOfWork.BeginDbTransaction();
        try
        {
            await RecalculateAllocationsAsync(group, oldUserAllocations);

            var userResult = await userService.GetOrCreateUserAsync(newUserTgId, newUserDisplayName);
            if (!userResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return userResult.SameFailure<Account>();
            }

            var user = userResult.Data;

            var newAccount = new Account
            {
                Role = newUserRole,
                DailyAllocation = newUserAllocation,
                SavingStrategy = newUserSavingStrategy,
                Balance = newUserAllocation,
                UserId = user.Id,
                User = user,
                GroupId = group.Id,
                Group = group
            };

            user.Accounts.Add(newAccount);
            group.Accounts.Add(newAccount);

            return Result<Account>.Success(newAccount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError("Something went wrong during add user to group: {errorMessage}", ex.Message);
            return Result<Account>.Failure(ex.Message);
        }
    }
}