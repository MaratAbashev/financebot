using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class BankInternalHttpService(HttpClient httpClient)
    : BaseHttpService(httpClient, "/InternalBank"), IInternalBankService
{
    public Task<Result<bool>> IsBankConnectedAsync(long userTgId, CancellationToken ct) =>
        PostAsync<bool>($"/Check?userTgId={userTgId}", ct);
}
