using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Groups")
            .WithTags("Group")
            .WithOpenApi();

        mapGroup.MapGet("/", GetAllGroupsAsync)
            .WithName("GetAllGroups")
            .WithDescription("Получить список всех групп")
            .Produces<List<Group>>();

        mapGroup.MapGet("/{groupId:Guid}", GetGroupByIdAsync)
            .WithName("GetGroupById")
            .WithDescription("Получить группу по ID")
            .Produces<Group>()
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapGet("/Users", GetUserGroupsAsync)
            .WithName("GetUserGroups")
            .WithDescription("Получить группы пользователя (опционально только те, где он админ)")
            .Produces<List<Group>>()
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/New", NewGroup)
            .WithName("CreateGroup")
            .WithDescription("Создать новую группу")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/RecalculateAllocations", RecalculateAllocations)
            .WithName("RecalculateAllocations")
            .WithDescription("Пересчитать распределение бюджета между участниками группы")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/AddUser", AddUser)
            .WithName("AddUserToGroup")
            .WithDescription("Добавить пользователя в группу и вернуть его счёт")
            .Produces<Account>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/RemoveUser", RemoveUser)
            .WithName("RemoveUserFromGroup")
            .WithDescription("Удалить пользователя из группы")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPatch("/ChangeGoal", ChangeGoal)
            .WithName("ChangeGroupGoal")
            .WithDescription("Изменить цель накоплений группы")
            .Produces<Saving>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPatch("/", UpdateGroup)
            .WithName("UpdateGroup")
            .WithDescription("Обновить параметры группы (название, пополнение, стратегии)")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPatch("/ToggleSaving", ToggleSaving)
            .WithName("ToggleGroupSaving")
            .WithDescription("Включить или выключить копилку группы")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllGroupsAsync(IGroupService groupService)
    {
        var result = await groupService.GetGroupsAsync();

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetGroupByIdAsync(
        Guid groupId,
        IGroupService groupService)
    {
        var result = await groupService.GetGroupByIdAsync(groupId);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetUserGroupsAsync(
        [FromQuery] long userTgId,
        [FromQuery] bool adminOnly,
        IGroupService groupService)
    {
        var result = await groupService.GetUserGroupsAsync(userTgId, adminOnly);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> NewGroup(
        [FromQuery] long userTgId,
        [FromBody] CreateGroupRequest request,
        IGroupService groupService)
    {
        var result = await groupService.CreateGroupAsync(request.GroupName,
            userTgId,
            request.Replenishment,
            request.GroupSavingStrategy,
            request.AccountSavingStrategy,
            request.DebtStrategy,
            request.SavingTargetName,
            request.SavingTargetAmount);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> RecalculateAllocations(
        [FromQuery] Guid groupId,
        [FromBody] RecalculateAllocationsRequest request,
        IGroupService groupService)
    {
        var result = await groupService.RecalculateMonthlyAllocationsAsync(groupId, request.Allocations);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> AddUser(
        [FromQuery] Guid groupId,
        [FromBody] AddUserToGroupRequest request,
        IGroupService groupService)
    {
        var result = await groupService.AddUserToGroupAsync(
            groupId,
            request.UserTgId,
            request.UserRole,
            request.OldUsersAllocations,
            request.NewUserAllocation,
            request.UserSavingStrategy);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> RemoveUser(
        [FromQuery] Guid groupId,
        [FromBody] RemoveUserRequest request,
        IGroupService groupService)
    {
        var result =
            await groupService.RemoveUserFromGroupAsync(groupId, request.UserTgId, request.OldUsersAllocations);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> ChangeGoal(
        [FromQuery] Guid groupId,
        [FromQuery] string targetName,
        [FromQuery] decimal targetCost,
        IGroupService groupService)
    {
        var result = await groupService.ChangeGoalAsync(groupId, targetName, targetCost);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> ToggleSaving(
        [FromQuery] Guid groupId,
        [FromQuery] bool savingFlag,
        IGroupService groupService)
    {
        var result = await groupService.ToggleSavingAsync(groupId, savingFlag);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> UpdateGroup(
        Guid groupId,
        [FromBody] UpdateGroupRequest request,
        IGroupService groupService)
    {
        var result = await groupService.UpdateGroupAsync(
            groupId,
            request.Name,
            request.MonthlyReplenishment,
            request.SavingStrategy,
            request.DebtStrategy);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }
}