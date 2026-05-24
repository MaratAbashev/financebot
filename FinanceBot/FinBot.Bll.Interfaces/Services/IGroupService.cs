using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupService
{
    Task<Result<Group>> CreateGroupAsync(
        string groupName,
        long creatorTgId,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount);

    Task<Result<Group>> UpdateGroupAsync(
        Guid groupId,
        string? name,
        decimal? monthlyReplenishment,
        SavingStrategy? savingStrategy,
        DebtStrategy? debtStrategy);

    Task<Result<Group>> ToggleSavingAsync(
        Guid groupId,
        bool savingFlag);

    Task<Result> RecalculateMonthlyAllocationsAsync(
        Guid groupId,
        decimal[] allocations);

    Task<Result<Saving>> ChangeGoalAsync(
        Guid groupId,
        string savingTargetName,
        decimal savingTargetAmount);

    Task<Result<Account>> AddUserToGroupAsync(
        Guid groupId,
        long userTgId,
        Role newUserRole,
        decimal[] oldUsersAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy);

    Task<Result> RemoveUserFromGroupAsync(
        Guid groupId,
        long userTgId,
        decimal[] leftUsersAllocations);

    Task<Result<IEnumerable<Group>>> GetGroupsAsync();

    Task<Result<Group>> GetGroupByIdAsync(Guid groupId);

    Task<Result<IEnumerable<Group>>> GetUserGroupsAsync(
        long userTgId,
        bool adminOnly,
        CancellationToken cancellationToken = default);
}