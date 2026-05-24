using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class HttpInvitationService(HttpClient httpClient)
    : BaseHttpService(httpClient, "/Invitations"), IInvitationService
{
    public Task<Result<string>> GenerateInviteCodeAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        GetAsync<string>($"/Generate?groupId={groupId}", cancellationToken);

    public Task<Result<Group>> JoinGroupByCodeAsync(
        long userTgId,
        string code,
        CancellationToken cancellationToken = default) =>
        PostAsync<Group, JoinGroupRequest>(
            "/Join",
            new JoinGroupRequest(userTgId, code),
            cancellationToken);

    public Task<Result<IEnumerable<User>>> GetPendingUsersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IEnumerable<User>>($"/Pending?groupId={groupId}", cancellationToken);

    public Task<Result> RemoveGroupInvitationsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/Remove?groupId={groupId}", cancellationToken);
}