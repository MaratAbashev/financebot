using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class HttpGroupService(HttpClient httpClient) : BaseHttpService(httpClient, "/Groups"), IGroupService
{
    public Task<Result<Group>> CreateGroupAsync(
        string groupName,
        long creatorTgId,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount) =>
        PostAsync<Group, CreateGroupRequest>(
            $"/New?userTgId={creatorTgId}",
            new CreateGroupRequest(
                groupName,
                replenishment,
                groupSavingStrategy,
                accountSavingStrategy,
                debtStrategy,
                savingTargetName,
                savingTargetAmount));

    public Task<Result<Group>> UpdateGroupAsync(
        Guid groupId,
        string? name,
        decimal? monthlyReplenishment,
        SavingStrategy? savingStrategy,
        DebtStrategy? debtStrategy) =>
        PatchAsync<Group, UpdateGroupRequest>(
            $"/?groupId={groupId}",
            new UpdateGroupRequest(name, monthlyReplenishment, savingStrategy, debtStrategy));

    public Task<Result<Group>> ToggleSavingAsync(
        Guid groupId,
        bool savingFlag) =>
        PatchAsync<Group>(
            $"/ToggleSaving?groupId={groupId}&savingFlag={savingFlag}");

    public Task<Result> RecalculateMonthlyAllocationsAsync(
        Guid groupId,
        decimal[] allocations) =>
        PostAsync(
            $"/RecalculateAllocations?groupId={groupId}",
            new RecalculateAllocationsRequest(allocations));

    public Task<Result<Saving>> ChangeGoalAsync(
        Guid groupId,
        string savingTargetName,
        decimal savingTargetAmount) =>
        PatchAsync<Saving>(
            $"/ChangeGoal?groupId={groupId}&targetName={Uri.EscapeDataString(savingTargetName)}&targetCost={savingTargetAmount}");

    public Task<Result<Account>> AddUserToGroupAsync(
        Guid groupId,
        long userTgId,
        Role newUserRole,
        decimal[] oldUsersAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy) =>
        PostAsync<Account, AddUserToGroupRequest>(
            $"/AddUser?groupId={groupId}",
            new AddUserToGroupRequest(
                userTgId,
                newUserRole,
                oldUsersAllocations,
                newUserAllocation,
                newUserSavingStrategy));

    public Task<Result> RemoveUserFromGroupAsync(
        Guid groupId,
        long userTgId,
        decimal[] leftUsersAllocations) =>
        PostAsync(
            $"/RemoveUser?groupId={groupId}",
            new RemoveUserRequest(userTgId, leftUsersAllocations));

    public Task<Result<IEnumerable<Group>>> GetGroupsAsync() =>
        GetAsync<IEnumerable<Group>>("/");

    public Task<Result<Group>> GetGroupByIdAsync(Guid groupId) =>
        GetAsync<Group>($"/{groupId}");

    public Task<Result<IEnumerable<Group>>> GetUserGroupsAsync(
        long userTgId,
        bool adminOnly,
        CancellationToken cancellationToken = default) =>
        GetAsync<IEnumerable<Group>>($"/Users?userTgId={userTgId}&adminOnly={adminOnly}", cancellationToken);
}