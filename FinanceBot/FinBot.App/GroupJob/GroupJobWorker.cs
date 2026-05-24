using FinBot.Bll.Interfaces.Services;

namespace FinBot.App.GroupJob;

public class GroupJobWorker(
    IGroupBackgroundService service,
    ILogger<GroupJobWorker> logger)
{
    public async Task ProcessMonthlyAsync(Guid groupId)
    {
        var serviceResult = await service.MonthlyGroupRefreshAsync(groupId);
        if (!serviceResult.IsSuccess)
        {
            logger.LogError("Failed to process monthly job for group {id}", groupId);
        }
        else
        {
            logger.LogInformation("Successfully processed monthly job for group {groupId}", groupId);
        }
    }
    
    public async Task ProcessDailyAsync(Guid groupId)
    {
        var serviceResult = await service.DailyAccountsRecalculateAsync(groupId);
        if (!serviceResult.IsSuccess)
        {
            logger.LogError("Failed to process daily job for group {id}", groupId);
        }
        else
        {
            logger.LogInformation("Successfully processed daily job for group {groupId}", groupId);
        }
    }
}
