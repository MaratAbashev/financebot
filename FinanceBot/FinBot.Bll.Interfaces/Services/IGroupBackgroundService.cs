using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupBackgroundService
{
    Task<Result> MonthlyGroupRefreshAsync(Group group);
    Task<Result> DailyAccountsRecalculateAsync(Group group);
}