using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupBackgroundService
{
    Task<Result> MonthlyGroupRefreshAsync(Guid groupId);
    Task<Result> DailyAccountsRecalculateAsync(Guid groupId);
}