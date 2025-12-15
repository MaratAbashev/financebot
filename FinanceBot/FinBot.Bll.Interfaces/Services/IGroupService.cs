using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Models.SavingModel;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupService
{
    Task<Result<Group>> CreateGroupAsync(string groupName,
        User creator,
        decimal replenishment,
        SavingStrategy groupSavingStrategy, SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount);
    
    Task<Result> RecalculateAllocationsAsync(
        Group group,
        decimal[] allocations);
    
    Task<Result<Saving>> ChangeGoalAsync(Group group, string savingTargetName, int savingTargetAmount);
    
    Task<Result<Account>> AddUserAsyncToGroup(Group group,
        Guid newUserId,
        long newUserTgId,
        string newUserDisplayName,
        Role newUserRole,
        decimal[] oldUserAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy);
}