using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.WebApi.GroupJob;

public class GroupJobWorker(
    IGroupBackgroundService service,
    IGenericRepository<Group, Guid, PDbContext> repository,
    ILogger<GroupJobWorker> logger)
{
    public async Task ProcessMonthlyAsync(Guid groupId)
    {
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .Where(g => g.Id == groupId)
            .FirstOrDefaultAsync();
        
        if (group == null) return;
        
        var serviceResult = await service.MonthlyGroupRefreshAsync(group);
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
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .Where(g => g.Id == groupId)
            .FirstOrDefaultAsync();
        
        if (group == null) return;

        var serviceResult = await service.DailyAccountsRecalculateAsync(group);
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
