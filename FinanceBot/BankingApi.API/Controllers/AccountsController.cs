using BankingApi.API.Extensions;
using BankingApi.API.Requests;
using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.API.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var accounts = await accountService.GetUserAccountsAsync(userId, ct);
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var account =
            await accountService.CreateAccountAsync(userId, new CreateAccountDto(request.Name, request.Currency), ct);
        
        return CreatedAtAction(nameof(GetAccounts), account);
    }

    [HttpGet("{accountId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid accountId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var balance = await accountService.GetBalanceAsync(accountId, userId, ct);
        return Ok(new { balance });
    }
}