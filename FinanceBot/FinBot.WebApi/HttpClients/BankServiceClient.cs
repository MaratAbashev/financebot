using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class BankServiceClient(HttpClient httpClient)
    : BaseHttpService(httpClient, "/bank"), IBankServiceClient
{
    public Task<Result<string>> GetAuthUrlAsync(Guid userId, CancellationToken ct) =>
        GetAsync<string>($"/auth-url/{userId}", ct);

    public Task<Result<int>> SynchronizeTransactionsAsync(Guid userId, CancellationToken ct) =>
        GetAsync<int>($"/transactions/sync/{userId}", ct);

    public Task<Result<string>> UnlinkBankAsync(Guid userId, CancellationToken ct) =>
        GetAsync<string>($"/auth/unlink/{userId}", ct);
}
