using FinBot.Bll.Interfaces;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace FinBot.WebApi.GroupJob;

public class GroupJobDispatcher(
    IBackgroundJobClient backgroundJobClient,
    IGenericRepository<Group, Guid, PDbContext> groupRepository,
    ILogger<GroupJobDispatcher> logger)
{
    public async Task DispatchTasksAsync()
    {
        var groupIds = await groupRepository.GetAll().Select(g => g.Id).ToListAsync();

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