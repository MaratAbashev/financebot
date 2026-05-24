using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Services;

public class AccountService(
    AppDbContext context,
    TimeProvider timeProvider) : IAccountService
{
    public async Task<IEnumerable<AccountDto>> GetUserAccountsAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new AccountDto(a.Id, a.Name, a.Balance, a.Currency.ToString(), a.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountDto dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<Currency>(dto.Currency, out var currency))
            throw new ArgumentException($"Неизвестная валюта: {dto.Currency}");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            Balance = 0,
            Currency = currency,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync(ct);

        return new AccountDto(account.Id, account.Name, account.Balance, account.Currency.ToString(), account.CreatedAt);
    }

    public async Task<decimal> GetBalanceAsync(Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var account = await context.Accounts
                          .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct)
                      ?? throw new KeyNotFoundException("Счёт не найден");

        return account.Balance;
    }
}