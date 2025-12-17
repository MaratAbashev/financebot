using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupService
{
    Task<Result<Group>> CreateGroupAsync(
        string groupName,
        User creator,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount);
    
    Task<Result> RecalculateMonthlyAllocationsAsync(
        Group group,
        decimal[] allocations);
    
    Task<Result<Saving>> ChangeGoalAsync(Group group, string savingTargetName, decimal savingTargetAmount);
    
    Task<Result<Account>> AddUserToGroupAsync(
        Group group,
        Guid userId,
        Role newUserRole,
        decimal[] oldUsersAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy);

    Task<Result> RemoveUserFromGroupAsync(
        Group group,
        long userTgId,
        decimal[] leftUsersAllocations);
}