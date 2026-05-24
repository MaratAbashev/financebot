using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Invitations")
            .WithTags("Invitations")
            .WithOpenApi();

        group.MapGet("/Generate", GenerateInviteCode)
            .WithName("GenerateInviteCode")
            .WithDescription("Сгенерировать код-приглашение для группы")
            .Produces<string>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/Join", JoinGroupByCode)
            .WithName("JoinGroupByCode")
            .WithDescription("Присоединиться к группе по коду-приглашению")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/Pending", GetPendingUsers)
            .WithName("GetPendingUsers")
            .WithDescription("Получить пользователей, ожидающих вступления в группу")
            .Produces<List<User>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/Remove", RemoveGroupInvitations)
            .WithName("RemoveGroupInvitations")
            .WithDescription("Удалить все заявки на вступление в группу")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GenerateInviteCode(
        [FromQuery] Guid groupId,
        IInvitationService invitationService)
    {
        var result = await invitationService.GenerateInviteCodeAsync(groupId);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> JoinGroupByCode(
        [FromBody] JoinGroupRequest request,
        IInvitationService invitationService)
    {
        var result = await invitationService.JoinGroupByCodeAsync(request.UserTgId, request.Code);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetPendingUsers(
        [FromQuery] Guid groupId,
        IInvitationService invitationService)
    {
        var result = await invitationService.GetPendingUsersAsync(groupId);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> RemoveGroupInvitations(
        [FromQuery] Guid groupId,
        IInvitationService invitationService)
    {
        var result = await invitationService.RemoveGroupInvitationsAsync(groupId);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToErrorHttpResult();
    }
}