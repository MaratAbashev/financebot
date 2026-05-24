using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class InvitationService(
    PDbContext dbContext,
    ILogger<InvitationService> logger) : IInvitationService
{
    public Task<Result<string>> GenerateInviteCodeAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<string>.Success(groupId.ToString()));
    }

    public async Task<Result<Group>> JoinGroupByCodeAsync(
        long userTgId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.TelegramId == userTgId, cancellationToken: cancellationToken);
        if (user is null)
        {
            logger.LogError("User with tgId {userTgId} does not exist", userTgId);
            return Result<Group>.Failure("User not exist", ErrorType.NotFound);
        }

        if (!Guid.TryParse(code, out var groupId))
        {
            return Result<Group>.Failure("Invalid invite code", ErrorType.Validation);
        }

        var group = await dbContext.Groups
            .Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken: cancellationToken);
        if (group is null)
        {
            logger.LogError("Invite code does not match any group: {groupId}", groupId);
            return Result<Group>.Failure("Invalid invite code", ErrorType.Validation);
        }

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.GroupId == group.Id,
                cancellationToken: cancellationToken);
        if (account is not null)
        {
            return Result<Group>.Failure("User already in the group", ErrorType.Conflict);
        }

        var request =
            await dbContext.JoinRequests.FirstOrDefaultAsync(x => x.UserId == user.Id && x.GroupId == groupId,
                cancellationToken: cancellationToken);
        if (request is not null)
        {
            return Result<Group>.Failure("User already has request to the group", ErrorType.Conflict);
        }

        var joinRequest = new JoinRequest
        {
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            GroupId = groupId,
        };

        await dbContext.JoinRequests.AddAsync(joinRequest, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Group>.Success(group);
    }

    public async Task<Result<IEnumerable<User>>> GetPendingUsersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await dbContext.JoinRequests
                .Where(jr => jr.GroupId == groupId)
                .Include(jr => jr.User)
                .AsNoTracking()
                .Select(jr => jr.User!)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<User>>.Success(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during get pending users for group {groupId}: {errorMessage}\nErrorStack{errorStack}",
                groupId, ex.Message, ex.StackTrace);
            return Result<IEnumerable<User>>.Failure("Failed to get pending users");
        }
    }

    public async Task<Result> RemoveGroupInvitationsAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groupExists = await dbContext.Groups
                .AnyAsync(g => g.Id == groupId, cancellationToken);
            if (!groupExists)
            {
                logger.LogError("Group not exist: {groupId}", groupId);
                return Result.Failure($"Group not exist: {groupId}", ErrorType.NotFound);
            }

            await dbContext.JoinRequests
                .Where(jr => jr.GroupId == groupId)
                .ExecuteDeleteAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to remove invitations for group {groupId}\n\tError: {errorMessage}\n\tStack: {stack}",
                groupId, ex.Message, ex.StackTrace);
            return Result.Failure($"Failed to remove invitations for group {groupId}");
        }
    }
}