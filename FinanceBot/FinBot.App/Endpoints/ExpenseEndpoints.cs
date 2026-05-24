using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Expenses")
            .WithTags("Expenses")
            .WithOpenApi();

        group.MapPost("/Add", AddExpense)
            .WithName("AddUserExpense")
            .WithDescription("Добавить трату пользователя и вернуть новый баланс счёта")
            .Produces<decimal>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/Pending", GetPendingExpenses)
            .WithName("GetPendingExpenses")
            .WithDescription("Получить нераспределённые траты пользователя (без groupId)")
            .Produces<List<Expense>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/Pending/Distribute", DistributeExpense)
            .WithName("DistributeExpense")
            .WithDescription("Распределить траты пользователя по группам")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddExpense(
        [FromQuery] long userTgId,
        [FromBody] AddExpenseRequest request,
        IExpenseService expenseService)
    {
        var result = await expenseService.AddExpenseAsync(
            userTgId,
            request.GroupId,
            request.Amount,
            request.Category);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetPendingExpenses(
        [FromQuery] long userTgId,
        IExpenseService expenseService)
    {
        var result = await expenseService.GetPendingExpensesAsync(userTgId);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> DistributeExpense(
        [FromQuery] long userTgId,
        [FromBody] DistributeExpenseRequest request,
        IExpenseService expenseService)
    {
        var result = await expenseService.DistributeExpensesAsync(
            userTgId,
            request.ExpenseId, request.GroupId, request.Category, request.Reject);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }
}