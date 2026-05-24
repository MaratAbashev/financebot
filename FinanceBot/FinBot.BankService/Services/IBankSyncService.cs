namespace FinBot.BankService.Services;

public interface IBankSyncService
{
    Task<int> SyncUserAsync(Guid userId, CancellationToken ct = default);
    Task SyncAllAsync(CancellationToken ct = default);
}