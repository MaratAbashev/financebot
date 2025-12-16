using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.TestEndpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/{tgId:long}", GetUserTg)
            .WithName("GetUserTg")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);
        
        group.MapGet("/{id:guid}", GetUserGuid)
            .WithName("GetUserGuid")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .Produces<User>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/ensure", GetOrCreateUser)
            .WithName("GetOrCreateUser")
            .Produces<User>();

        group.MapPost("/{tgId:long}/expenses", AddExpense)
            .WithName("AddUserExpense")
            .Produces<decimal>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }
    

    private static async Task<IResult> GetUserTg(
        long tgId, 
        IUserService userService)
    {
        var result = await userService.GetUserAsync(u => u.TelegramId == tgId);

        if (!result.IsSuccess)
        {
            return Results.Problem(result.ErrorMessage);
        }

        if (result.Data == null)
        {
            return Results.NotFound("User not found");
        }

        return Results.Ok(result.Data);
    }
    
    private static async Task<IResult> GetUserGuid(
        Guid id, 
        IUserService userService)
    {
        var result = await userService.GetUserAsync(u => u.Id == id);
        
        if (!result.IsSuccess)
        {
            return Results.Problem(result.ErrorMessage);
        }

        if (result.Data == null)
        {
            return Results.NotFound("User not found");
        }

        return Results.Ok(result.Data);
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserRequest request, 
        IUserService userService)
    {
        var result = await userService.CreateUserAsync(request.TgId, request.DisplayName);

        if (!result.IsSuccess)
        {
            return Results.Problem(result.ErrorMessage);
        }

        return Results.Ok(result.Data);
    }

    private static async Task<IResult> GetOrCreateUser(
        [FromBody] CreateUserRequest request, 
        IUserService userService)
    {
        var result = await userService.GetOrCreateUserAsync(request.TgId, request.DisplayName);
        
        return result.IsSuccess 
            ? Results.Ok(result.Data) 
            : Results.BadRequest(result.ErrorMessage);
    }

    private static async Task<IResult> AddExpense(
        long tgId, 
        [FromBody] AddExpenseRequest request, 
        IUserService userService)
    {
        var userResult = await userService.GetUserAsync(u => u.TelegramId == tgId);

        if (!userResult.IsSuccess)
        {
            return Results.Problem(userResult.ErrorMessage);
        }
        
        var user = userResult.Data;
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        var expenseResult = await userService.AddExpenseAsync(
            user, 
            request.GroupId, 
            request.Amount, 
            request.Category
        );

        if (!expenseResult.IsSuccess)
        {
            return Results.BadRequest(expenseResult.ErrorMessage);
        }

        return Results.Ok(new { NewBalance = expenseResult.Data });
    }
}

public record CreateUserRequest(long TgId, string DisplayName);

public record AddExpenseRequest(Guid GroupId, decimal Amount, ExpenseCategory Category);