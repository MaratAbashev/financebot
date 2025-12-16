using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.WebApi.TestEndpoints;

public static class BackgroundEndpoints
{
    public static void MapBackgroundEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Background")
            .WithTags("Background Jobs")
            .WithOpenApi();

        group.MapPost("/Groups/{groupId:guid}/monthly-refresh", TriggerMonthlyRefresh)
            .WithName("TriggerMonthlyGroupRefresh");

        group.MapPost("/Groups/{groupId:guid}/daily-recalculate", TriggerDailyRecalculate)
            .WithName("TriggerDailyAccountsRecalculate");
    }

    private static async Task<IResult> TriggerMonthlyRefresh(
        Guid groupId,
        IGroupBackgroundService backgroundService,
        IGenericRepository<Group, Guid, PDbContext> repository)
    {
        var group = await GetGroupWithIncludes(repository, groupId);
        
        if (group is null)
        {
            return Results.NotFound($"Group with ID {groupId} not found.");
        }

        await backgroundService.MonthlyGroupRefreshAsync(group);

        return Results.Ok($"Monthly refresh for group {group.Id} triggered successfully.");
    }

    private static async Task<IResult> TriggerDailyRecalculate(
        Guid groupId,
        IGroupBackgroundService backgroundService,
        IGenericRepository<Group, Guid, PDbContext> repository)
    {
        var group = await GetGroupWithIncludes(repository, groupId);

        if (group is null)
        {
            return Results.NotFound($"Group with ID {groupId} not found.");
        }

        await backgroundService.DailyAccountsRecalculateAsync(group);

        return Results.Ok($"Daily recalculation for group {group.Id} triggered successfully.");
    }

    private static async Task<Group?> GetGroupWithIncludes(
        IGenericRepository<Group, Guid, PDbContext> repository, 
        Guid groupId)
    {
        return await repository.GetAll()
            .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }
}
