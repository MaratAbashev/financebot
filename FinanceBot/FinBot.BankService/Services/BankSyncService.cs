using FinBot.BankService.BankingApi;
using FinBot.BankService.Cache;
using FinBot.BankService.Kafka;
using FinBot.BankService.Mappers;
using FinBot.BankService.Models;
using FinBot.BankService.Repositories;
using FinBot.Domain.Models;

namespace FinBot.BankService.Services;

public class BankSyncService(
    IBankConnectionRepository connections,
    IBankTransactionRepository transactions,
    ITokenCache tokenCache,
    IBankingApiClient bankingApi,
    IExpenseWriteRepository expenseRepository,
    TimeProvider timeProvider,
    ILogger<BankSyncService> logger) : IBankSyncService
{
    public async Task<int> SyncUserAsync(Guid userId, CancellationToken ct = default)
    {
        var accessToken = await GetOrRefreshAccessTokenAsync(userId, ct);
        if (accessToken is null)
        {
            logger.LogWarning("Не удалось получить access token для UserId={UserId}", userId);
            return 0;
        }

        var accountsResult = await bankingApi.GetAccountsAsync(accessToken, ct);
        if (!accountsResult.IsSuccess)
        {
            logger.LogError("Ошибка получения счетов для UserId={UserId}: {Error}",
                userId, accountsResult.ErrorMessage);
            return 0;
        }

        var newCount = 0;
        foreach (var account in accountsResult.Data)
        {
            var txResult = await bankingApi.GetTransactionsAsync(account.Id, accessToken, ct);
            if (!txResult.IsSuccess) continue;

            foreach (var tx in txResult.Data)
            {
                if (await transactions.ExistsAsync(tx.Id, ct))
                    continue;

                await transactions.AddRangeAsync([new BankTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ExternalId = tx.Id,
                    Amount = tx.Amount,
                    Type = tx.Type,
                    CategoryName = tx.CategoryName,
                    Description = tx.Description,
                    CreatedAt = tx.CreatedAt,
                    Status = BankTransactionStatus.Pending
                }], ct);

                if (tx.Type == "Expense")
                {
                    var category = CategoryMapper.Map(tx.CategoryName);
                    await expenseRepository.AddAsync(userId, tx.Amount, category, tx.CreatedAt, ct);
                }

                newCount++;
            }
        }

        return newCount;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        var activeConnections = await connections.GetAllActiveAsync(ct);
        logger.LogInformation("Периодическая синхронизация {Count} подключений", activeConnections.Count);

        foreach (var connection in activeConnections)
            await SyncUserAsync(connection.UserId, ct);
    }

    private async Task<string?> GetOrRefreshAccessTokenAsync(Guid userId, CancellationToken ct)
    {
        var cached = await tokenCache.GetAsync(userId);
        if (cached is not null)
            return cached;

        var connection = await connections.GetByUserIdAsync(userId, ct);
        if (connection is null) 
            return null;

        var refreshResult = await bankingApi.RefreshTokenAsync(connection.RefreshToken, ct);
        if (!refreshResult.IsSuccess) 
            return null;

        await tokenCache.SetAsync(userId, refreshResult.Data, TimeSpan.FromMinutes(15));
        await connections.UpdateRefreshTokenAsync(userId, connection.RefreshToken, ct);

        return refreshResult.Data;
    }
}