using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class GroupService(
    PDbContext dbContext,
    ILogger<GroupService> logger) : IGroupService
{
    public async Task<Result<Group>> CreateGroupAsync(
        string groupName,
        long creatorTgId,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount)
    {
        try
        {
            if (groupName.Length is 0 or > 20)
            {
                return Result<Group>.Failure("Group name has invalid length", ErrorType.Validation);                
            }

            if (savingTargetName?.Length is 0 or > 20)
            {
                return Result<Group>.Failure("Saving target name has invalid length", ErrorType.Validation);                
            }
            
            if (replenishment <= 0)
            {
                return Result<Group>.Failure("Monthly replenishment is zero or less", ErrorType.Validation);                
            }

            var now = DateTime.UtcNow;
            var daysInMonthLeft = DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);
            var dailyUserAllocation = Math.Round(replenishment / daysInMonthLeft, 2, MidpointRounding.ToZero);
            var todayGroupBalance = replenishment - dailyUserAllocation;

            var creator = await dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == creatorTgId);
            if (creator is null)
            {
                logger.LogError("User {creatorTgId} does not exist", creatorTgId);
                return Result<Group>.Failure("User does not exist", ErrorType.NotFound);
            }

            var newGroup = new Group
            {
                Name = groupName,
                GroupBalance = todayGroupBalance,
                MonthlyReplenishment = replenishment,
                SavingStrategy = groupSavingStrategy,
                DebtStrategy = debtStrategy,
                Accounts =
                [
                ]
            };

            var newSaving = new Saving
            {
                Name = savingTargetName ?? "None",
                TargetAmount = savingTargetAmount ?? -1,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = now,
            };

            var newAccount = new Account
            {
                Role = Role.Admin,
                DailyAllocation = dailyUserAllocation,
                MonthlyAllocation = replenishment,
                SavingStrategy = accountSavingStrategy,
                Balance = dailyUserAllocation
            };

            newGroup.Saving = newSaving;
            newGroup.Accounts.Add(newAccount);

            creator.Groups.Add(newGroup);
            creator.Accounts.Add(newAccount);

            await dbContext.SaveChangesAsync();

            return Result<Group>.Success(newGroup);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during create group: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Group>.Failure("Failed to create group");
        }
    }

    public async Task<Result<Group>> UpdateGroupAsync(
        Guid groupId,
        string? name,
        decimal? monthlyReplenishment,
        SavingStrategy? savingStrategy,
        DebtStrategy? debtStrategy)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            if (name?.Length is 0 or > 20)
            {
                return Result<Group>.Failure("Group name has invalid length", ErrorType.Validation);                
            }

            if (monthlyReplenishment <= 0)
            {
                return Result<Group>.Failure("Monthly replenishment is zero or less", ErrorType.Validation);                
            }
            
            var group = await dbContext.Groups
                .Include(g => g.Accounts)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<Group>.Failure("Group does not exist", ErrorType.NotFound);
            }

            group.Name = name ?? group.Name;
            group.MonthlyReplenishment = monthlyReplenishment ?? group.MonthlyReplenishment;
            group.SavingStrategy = savingStrategy ?? group.SavingStrategy;
            group.DebtStrategy = debtStrategy ?? group.DebtStrategy;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<Group>.Success(group);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(
                ex,
                "Something went wrong during editing group: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Group>.Failure("Failed to update group");
        }
    }

    public async Task<Result<Group>> ToggleSavingAsync(Guid groupId, bool savingFlag)
    {
        try
        {
            var group = await dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<Group>.Failure("Group does not exist", ErrorType.NotFound);
            }

            if (savingFlag)
            {
                group.SavingStrategy = SavingStrategy.Save;
                group.DebtStrategy = DebtStrategy.FromSaving;
            }
            else
            {
                if (group.SavingStrategy == SavingStrategy.Save)
                    group.SavingStrategy = SavingStrategy.SaveForNextPeriod;
                if (group.DebtStrategy == DebtStrategy.FromSaving)
                    group.DebtStrategy = DebtStrategy.Nullify;
            }

            await dbContext.SaveChangesAsync();

            return Result<Group>.Success(group);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during toggle saving: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Group>.Failure("Failed to toggle saving");
        }
    }

    public async Task<Result> RecalculateMonthlyAllocationsAsync(Guid groupId, decimal[] allocations)
    {
        try
        {
            var result = await RecalculateMonthlyAllocationsCoreAsync(groupId, allocations);
            if (!result.IsSuccess)
                return result;

            await dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Something went wrong during recalculate allocations: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result.Failure("Failed to recalculate allocations");
        }
    }

    private async Task<Result> RecalculateMonthlyAllocationsCoreAsync(Guid groupId, decimal[] allocations, Guid? deletedUserId = null)
    {
        var now = DateTime.UtcNow;
        var daysInMonthLeft = DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);

        var group = await dbContext.Groups
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
        {
            logger.LogError("Group {groupId} does not exist", groupId);
            return Result.Failure("Group does not exist", ErrorType.NotFound);
        }

        var accounts = group.Accounts.OrderBy(a => a.User!.TelegramId).ToList();
        if (deletedUserId is not null)
            accounts.Remove(accounts.First(a => a.User!.Id == deletedUserId.Value));
        for (var i = 0; i < accounts.Count; i++)
        {
            accounts[i].MonthlyAllocation = allocations[i];
            accounts[i].DailyAllocation = Math.Round(allocations[i] / daysInMonthLeft, 2, MidpointRounding.ToZero);
        }

        return Result.Success();
    }

    public async Task<Result<Saving>> ChangeGoalAsync(Guid groupId, string savingTargetName, decimal savingTargetAmount)
    {
        try
        {
            var group = await dbContext.Groups
                .Include(g => g.Saving)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<Saving>.Failure("Group does not exist", ErrorType.NotFound);
            }

            var saving = group.Saving!;
            var leftover = saving.TargetAmount <= saving.CurrentAmount
                ? saving.TargetAmount - saving.CurrentAmount
                : saving.CurrentAmount;

            saving.Name = savingTargetName;
            saving.TargetAmount = savingTargetAmount;
            saving.CurrentAmount = leftover;
            saving.CreatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Result<Saving>.Success(saving);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Something went wrong during change goal: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Saving>.Failure("Failed to change goal");
        }
    }

    public async Task<Result<Account>> AddUserToGroupAsync(Guid groupId,
        long userTgId,
        Role newUserRole,
        decimal[] oldUserAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var recalculateResult = await RecalculateMonthlyAllocationsCoreAsync(groupId, oldUserAllocations);
            if (!recalculateResult.IsSuccess)
            {
                logger.LogError(
                    "Failed to recalculate user allocations before adding new user: {recalculateResultErrorMessage}",
                    recalculateResult.ErrorMessage);
                return Result<Account>.Failure(recalculateResult.ErrorMessage!, recalculateResult.ErrorType);
            }

            var user = await dbContext.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.TelegramId == userTgId);
            if (user is null)
            {
                logger.LogError("User with tgId {userTgId} does not exist", userTgId);
                return Result<Account>.Failure("User not exist", ErrorType.NotFound);
            }

            var group = await dbContext.Groups
                .Include(g => g.Saving)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<Account>.Failure("Group does not exist", ErrorType.NotFound);
            }

            var joinRequest = await dbContext.JoinRequests
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.GroupId == group.Id);
            if (joinRequest is not null)
            {
                dbContext.JoinRequests.Remove(joinRequest);
            }

            if (user.Accounts.Any(a => a.GroupId == groupId))
            {
                logger.LogError("User {userId} already added to the group {groupId}", user.Id, groupId);
                return Result<Account>.Failure("User already added to the group", ErrorType.Conflict);
            }

            var now = DateTime.UtcNow;
            var daysInMonthLeft = DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);
            var dailyUserAllocation = Math.Round(newUserAllocation / daysInMonthLeft, 2, MidpointRounding.ToZero);
            group.GroupBalance -= dailyUserAllocation;

            var newAccount = new Account
            {
                Role = newUserRole,
                DailyAllocation = dailyUserAllocation,
                MonthlyAllocation = newUserAllocation,
                SavingStrategy = newUserSavingStrategy,
                Balance = dailyUserAllocation
            };

            user.Accounts.Add(newAccount);
            group.Accounts.Add(newAccount);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<Account>.Success(newAccount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex,
                "Something went wrong during add user to group: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Account>.Failure("Failed to add user to group");
        }
    }

    public async Task<Result> RemoveUserFromGroupAsync(Guid groupId, long userTgId, decimal[] leftUsersAllocations)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var user = await dbContext.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.TelegramId == userTgId);
            if (user is null)
            {
                logger.LogError("User with tgId {userTgId} does not exist", userTgId);
                return Result.Failure("User not exist", ErrorType.NotFound);
            }

            var group = await dbContext.Groups
                .Include(g => g.Accounts)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result.Failure("Group does not exist", ErrorType.NotFound);
            }

            var userAccount = group.Accounts.FirstOrDefault(a => a.UserId == user.Id);
            if (userAccount is null)
            {
                logger.LogError("User {userId} doesn't has an Account in group {groupId}", user.Id, groupId);
                return Result.Failure("User not exist", ErrorType.NotFound);
            }

            dbContext.Accounts.Remove(userAccount);

            var recalculateResult =
                await RecalculateMonthlyAllocationsCoreAsync(groupId, leftUsersAllocations, user.Id);
            if (!recalculateResult.IsSuccess)
            {
                logger.LogError(
                    "Failed to recalculate  user allocations after adding new user: {recalculateResultErrorMessage}",
                    recalculateResult.ErrorMessage);
                return recalculateResult;
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex,
                "Something went wrong during remove user from group: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result.Failure("Failed to remove user from group");
        }
    }

    public async Task<Result<IEnumerable<Group>>> GetGroupsAsync()
    {
        try
        {
            return Result<IEnumerable<Group>>.Success(await dbContext.Groups
                .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
                .Include(g => g.Saving)
                .ToListAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during getting groups: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<IEnumerable<Group>>.Failure("Failed to get groups");
        }
    }

    public async Task<Result<Group>> GetGroupByIdAsync(Guid groupId)
    {
        try
        {
            var group = await dbContext.Groups
                .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
                .Include(g => g.Saving)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<Group>.Failure("Group does not exist", ErrorType.NotFound);
            }

            return Result<Group>.Success(group);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during getting group: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<Group>.Failure("Failed to get group");
        }
    }

    public async Task<Result<IEnumerable<Group>>> GetUserGroupsAsync(
        long userTgId,
        bool adminOnly,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await dbContext.Users
                .Include(u => u.Accounts)
                .ThenInclude(a => a.Group)
                .ThenInclude(g => g!.Saving)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TelegramId == userTgId, cancellationToken);

            if (user is null)
            {
                logger.LogError("User with tgId {userTgId} does not exist", userTgId);
                return Result<IEnumerable<Group>>.Failure("User not exist", ErrorType.NotFound);
            }

            var accounts = adminOnly
                ? user.Accounts.Where(a => a.Role == Role.Admin)
                : user.Accounts;

            var groups = accounts
                .Where(a => a.Group != null)
                .Select(a => a.Group!)
                .ToList();

            return Result<IEnumerable<Group>>.Success(groups);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during getting user groups: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<IEnumerable<Group>>.Failure("Failed to get user groups");
        }
    }
}