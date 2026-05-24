using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Services;

public class TransactionService(
    AppDbContext context,
    TimeProvider timeProvider) : ITransactionService
{
    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(
        Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var accountExists = await context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);

        if (!accountExists)
            throw new KeyNotFoundException("Счёт не найден");

        return await context.Transactions
            .Where(t => t.AccountId == accountId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Type.ToString(),
                t.Category.Name,
                t.Description,
                t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TransactionDto> CreateTransactionAsync(
        Guid accountId, Guid userId, CreateTransactionDto request, CancellationToken ct = default)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Счёт не найден");

        if (!Enum.TryParse<TransactionType>(request.Type, out var type))
            throw new ArgumentException($"Неизвестный тип транзакции: {request.Type}");

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new KeyNotFoundException("Категория не найдена");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Type = type,
            Description = request.Description,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        account.Balance += type == TransactionType.Income
            ? request.Amount
            : -request.Amount;

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(ct);

        return new TransactionDto(
            transaction.Id,
            transaction.Amount,
            transaction.Type.ToString(),
            category.Name,
            transaction.Description,
            transaction.CreatedAt);
    }

    public async Task<SummaryDto> GetSummaryAsync(
        Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var accountExists = await context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);

        if (!accountExists)
            throw new KeyNotFoundException("Счёт не найден");

        var transactions = await context.Transactions
            .Where(t => t.AccountId == accountId)
            .Include(t => t.Category)
            .ToListAsync(ct);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var byCategory = transactions
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategorySummaryDto(g.Key, g.Sum(t => t.Amount)));

        return new SummaryDto(totalIncome, totalExpense, totalIncome - totalExpense, byCategory);
    }
}