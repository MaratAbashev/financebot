using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IInvitationService
{
    Task<Result<string>> GenerateInviteCodeAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<Result<Group>> JoinGroupByCodeAsync(long userTgId, string code, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<User>>> GetPendingUsersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<Result> RemoveGroupInvitationsAsync(Guid groupId, CancellationToken cancellationToken = default);
}