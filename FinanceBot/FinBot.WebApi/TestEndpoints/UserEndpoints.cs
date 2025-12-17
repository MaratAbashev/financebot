using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinBot.WebApi.TestEndpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/{id:long}", GetUserTg)
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

        group.MapPost("/{id:long}/expenses", AddExpense)
            .WithName("AddUserExpense")
            .Produces<decimal>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }
    

    private static async Task<IResult> GetUserTg(
        long id, 
        IGenericRepository<User, Guid, PDbContext> userRepository)
    {
        var user = await userRepository.GetAll()
            .Include(u => u.Accounts)
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.TelegramId == id);

        if (user == null)
        {
            return Results.NotFound("User not found");
        }

        return Results.Ok(user);
    }
    
    private static async Task<IResult> GetUserGuid(
        Guid id, 
        IGenericRepository<User, Guid, PDbContext> userRepository)
    {
        var user = await userRepository.GetAll()
            .Include(u => u.Accounts)
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return Results.NotFound("User not found");
        }

        return Results.Ok(user);
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
        long id, 
        [FromBody] AddExpenseRequest request,
        IUserService userService,
        IGenericRepository<User, Guid, PDbContext> userRepository)
    {
        var user = await userRepository.GetAll()
            .Include(u => u.Accounts)
            .ThenInclude(a => a.Expenses)
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.TelegramId == id);

        if (user == null)
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