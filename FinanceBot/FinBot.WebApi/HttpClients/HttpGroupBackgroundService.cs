using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class HttpGroupBackgroundService(HttpClient httpClient)
    : BaseHttpService(httpClient, "/Background"), IGroupBackgroundService
{
    public Task<Result> MonthlyGroupRefreshAsync(Guid groupId) =>
        PostAsync($"/MonthlyRefresh?groupId={groupId}");

    public Task<Result> DailyAccountsRecalculateAsync(Guid groupId) =>
        PostAsync($"/DailyRecalculate?groupId={groupId}");
}