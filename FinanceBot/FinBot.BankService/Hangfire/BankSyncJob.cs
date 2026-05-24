using FinBot.BankService.Services;

namespace FinBot.BankService.Hangfire;

public class BankSyncJob(IBankSyncService syncService, ILogger<BankSyncJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Запуск периодической синхронизации банковских транзакций");
        await syncService.SyncAllAsync();
    }
}