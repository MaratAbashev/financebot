using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinBot.WebApi.TestEndpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Groups")
            .WithTags("Group")
            .WithOpenApi();

        mapGroup.MapGet("/",
                async (IGenericRepository<Group, Guid, PDbContext> repository) =>
                    Results.Ok(await repository.GetAll()
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .Include(g => g.Saving)
                        .ToListAsync()))
            .Produces<List<Group>>();

        mapGroup.MapGet("/{groupId:Guid}",
                async (Guid groupId, IGenericRepository<Group, Guid, PDbContext> repository) =>
                    Results.Ok(await repository.GetAll()
                        .Include(g => g.Accounts)
                        .ThenInclude(a => a.User)
                        .Include(g => g.Saving)
                        .FirstOrDefaultAsync(g => g.Id == groupId)))
            .Produces<Group>();

        mapGroup.MapPost("/New/Guid", NewGroupWithGuidUser)
            .Produces<Group>();

        mapGroup.MapPost("/New/Long", NewGroupWithLongUser)
            .Produces<Group>();

        mapGroup.MapPost("/RecalculateAllocations", RecalculateAllocations);

        mapGroup.MapPost("/AddUser", AddUser)
            .Produces<Account>();
        
        mapGroup.MapPost("/RemoveUser", RemoveUser)
            .Produces<Account>();

        mapGroup.MapPatch("/ChangeGoal", ChangeGoal)
            .Produces<Saving>();
        
        mapGroup.MapPatch("/", UpdateGroup)
            .Produces<Group>();
    }

    private static async Task<IResult> NewGroupWithLongUser([FromQuery] long userId, [FromBody] CreateGroupDto dto,
        IGroupService groupService, IGenericRepository<User, Guid, PDbContext> userRepository)
    {
        var user = await userRepository.GetAll()
            .Include(u => u.Accounts)
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.TelegramId == userId);
        
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        var newGroupResult = await groupService.CreateGroupAsync(dto.GroupName, user, dto.Replenishment,
            dto.GroupSavingStrategy, dto.AccountSavingStrategy, dto.DebtStrategy, dto.SavingTargetName,
            dto.SavingTargetAmount);
        if (!newGroupResult.IsSuccess)
        {
            return Results.Problem(newGroupResult.ErrorMessage);
        }

        return Results.Ok(newGroupResult.Data);
    }

    private static async Task<IResult> NewGroupWithGuidUser([FromQuery] Guid userId, [FromBody] CreateGroupDto dto,
        IGroupService groupService, IGenericRepository<User, Guid, PDbContext> userRepository)
    {
        var user = await userRepository.GetAll()
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return Results.NotFound("User not found");
        }
        
        var newGroupResult = await groupService.CreateGroupAsync(dto.GroupName, user, dto.Replenishment,
            dto.GroupSavingStrategy, dto.AccountSavingStrategy, dto.DebtStrategy, dto.SavingTargetName,
            dto.SavingTargetAmount);
        if (!newGroupResult.IsSuccess)
        {
            return Results.Problem(newGroupResult.ErrorMessage);
        }

        return Results.Ok(newGroupResult.Data);
    }

    private static async Task<IResult> RecalculateAllocations([FromQuery] Guid groupId,
        [FromBody] RecalculateAllocationsDto dto,
        IGenericRepository<Group, Guid, PDbContext> repository, IGroupService groupService)
    {
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return Results.NotFound("Group not found");
        }

        var allocationsResult = await groupService.RecalculateMonthlyAllocationsAsync(group, dto.Allocations);

        return allocationsResult.IsSuccess
            ? Results.Ok()
            : Results.Problem();
    }

    private static async Task<IResult> AddUser([FromQuery] Guid groupId, [FromBody] AddUserToGroupDto dto,
        IGenericRepository<Group, Guid, PDbContext> repository, IGroupService groupService)
    {
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        
        if (group == null)
        {
            return Results.NotFound("Group not found");
        }

        if (group.Accounts.Any(a => a.User!.Id == dto.UserId))
        {
            return Results.Ok("User already added");
        }

        var addUserResult = await groupService.AddUserToGroupAsync(group,
            dto.UserId,
            dto.UserRole,
            dto.OldUsersAllocations,
            dto.NewUserAllocation,
            dto.UserSavingStrategy);
        if (!addUserResult.IsSuccess)
        {
            return Results.Problem(addUserResult.ErrorMessage);
        }

        var newUserAccount = addUserResult.Data;

        return Results.Ok(newUserAccount);
    }
    
    private static async Task<IResult> RemoveUser([FromQuery] Guid groupId, [FromBody] RemoveUserDto dto,
        IGenericRepository<Group, Guid, PDbContext> repository, IGroupService groupService)
    {
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
                .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return Results.NotFound("Group not found");
        }

        var removeUserResult = await groupService.RemoveUserFromGroupAsync(group, dto.UserTgId, dto.OldUsersAllocations);
        return removeUserResult.IsSuccess
            ? Results.Ok()
            : Results.Problem(removeUserResult.ErrorMessage);
    }
    
    private static async Task<IResult> ChangeGoal([FromQuery] Guid groupId, [FromQuery] string targetName,
        [FromQuery] decimal targetCost, IGenericRepository<Group, Guid, PDbContext> repository,
        IGroupService groupService)
    {
        var group = await repository.GetAll()
            .Include(g => g.Accounts)
            .ThenInclude(a => a.User)
            .Include(g => g.Saving)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return Results.NotFound("Group not found");
        }

        var serviceResult = await groupService.ChangeGoalAsync(group, targetName, targetCost);

        return serviceResult.IsSuccess
            ? Results.Ok(serviceResult.Data)
            : Results.Problem(serviceResult.ErrorMessage);
    }

    private static async Task<IResult> UpdateGroup(Guid groupId, [FromBody] UpdateGroupDto dto,
        IGenericRepository<Group, Guid, PDbContext> repository)
    {
        var group = await repository.FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return Results.NotFound("Group not found");
        }
        
        group.Name = dto.Name ?? group.Name;
        group.MonthlyReplenishment = dto.MonthlyReplenishment ?? group.MonthlyReplenishment;
        group.SavingStrategy = dto.SavingStrategy ?? group.SavingStrategy;
        group.DebtStrategy = dto.DebtStrategy ?? group.DebtStrategy;
        
        repository.Update(group);
        await repository.SaveChangesAsync();

        return Results.Ok(group);
    }
}

public record CreateGroupDto(
    string GroupName,
    decimal Replenishment,
    SavingStrategy GroupSavingStrategy,
    SavingStrategy AccountSavingStrategy,
    DebtStrategy DebtStrategy,
    string? SavingTargetName,
    decimal? SavingTargetAmount);

public record RecalculateAllocationsDto(decimal[] Allocations);

public record AddUserToGroupDto(
    Guid UserId,
    Role UserRole,
    decimal[] OldUsersAllocations,
    decimal NewUserAllocation,
    SavingStrategy UserSavingStrategy);
    
public record UpdateGroupDto(
    string? Name,
    decimal? MonthlyReplenishment,
    SavingStrategy? SavingStrategy,
    DebtStrategy? DebtStrategy);
    
public record RemoveUserDto(
    long UserTgId,
    decimal[] OldUsersAllocations);