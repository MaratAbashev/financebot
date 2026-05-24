using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/{id:long}", GetUserTg)
            .WithName("GetUserTg")
            .WithDescription("Получить пользователя по Telegram ID")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetUserGuid)
            .WithName("GetUserGuid")
            .WithDescription("Получить пользователя по GUID")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithDescription("Создать нового пользователя")
            .Produces<User>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/ensure", GetOrCreateUser)
            .WithName("GetOrCreateUser")
            .WithDescription("Получить существующего пользователя или создать нового")
            .Produces<User>()
            .Produces(StatusCodes.Status400BadRequest);
    }


    private static async Task<IResult> GetUserTg(
        long id,
        IUserService userService)
    {
        var result = await userService.GetUserByTgIdAsync(id);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetUserGuid(
        Guid id,
        IUserService userService)
    {
        var result = await userService.GetUserByGuidIdAsync(id);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserRequest request,
        IUserService userService)
    {
        var result = await userService.CreateUserAsync(request.TgId, request.DisplayName);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetOrCreateUser(
        [FromBody] CreateUserRequest request,
        IUserService userService)
    {
        var result = await userService.GetOrCreateUserAsync(request.TgId, request.DisplayName);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }
}
