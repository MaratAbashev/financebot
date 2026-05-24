using BankingApi.API.Extensions;
using BankingApi.API.Requests;
using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.API.Controllers;

[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Authorize]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTransactions(Guid accountId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var transactions = await transactionService.GetTransactionsAsync(accountId, userId, ct);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        Guid accountId,
        [FromBody] CreateTransactionRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var dto = new CreateTransactionDto(request.CategoryId, request.Amount, request.Type, request.Description);
        
        var transaction = await transactionService.CreateTransactionAsync(accountId, userId, dto, ct);
        return CreatedAtAction(
            nameof(GetTransactions),
            new { accountId },
            transaction);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(Guid accountId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var summary = await transactionService.GetSummaryAsync(accountId, userId, ct);
        return Ok(summary);
    }
}