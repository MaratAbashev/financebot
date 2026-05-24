using FinBot.Dal.DbContexts;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace FinBot.App.GroupJob;

public class GroupJobDispatcher(
    IBackgroundJobClient backgroundJobClient,
    PDbContext dbContext,
    ILogger<GroupJobDispatcher> logger)
{
    public async Task DispatchTasksAsync()
    {
        var groupIds = await dbContext.Groups.Select(g => g.Id).ToListAsync();

        var now = DateTime.UtcNow;
        var isFirstDayOfMonth = now.Day == 1;

        foreach (var groupId in groupIds)
        {
            if (isFirstDayOfMonth)
            {
                logger.LogInformation("Dispatching monthly job for group {groupId}", groupId);
                backgroundJobClient.Enqueue<GroupJobWorker>(w => w.ProcessMonthlyAsync(groupId));
            }
            else
            {
                logger.LogInformation("Dispatching daily job for group {groupId}", groupId);
                backgroundJobClient.Enqueue<GroupJobWorker>(w => w.ProcessDailyAsync(groupId));
            }
        }
    }
}