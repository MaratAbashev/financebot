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
        var result = await backgroundService.MonthlyGroupRefreshAsync(groupId);

        return result.IsSuccess
            ? Results.Ok($"Monthly refresh for group {groupId} triggered successfully.")
            : Results.Problem($"Monthly refresh for group {groupId} triggered failed.");
    }

    private static async Task<IResult> TriggerDailyRecalculate(
        Guid groupId,
        IGroupBackgroundService backgroundService,
        IGenericRepository<Group, Guid, PDbContext> repository)
    {
        var result = await backgroundService.DailyAccountsRecalculateAsync(groupId);

        return result.IsSuccess
            ? Results.Ok($"Daily recalculation for group {groupId} triggered successfully.")
            : Results.Problem($"Daily recalculation for group {groupId} triggered failed.");
    }
}
