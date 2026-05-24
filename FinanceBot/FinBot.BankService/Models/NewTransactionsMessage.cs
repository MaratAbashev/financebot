namespace FinBot.BankService.Models;

public record NewTransactionsMessage(
    Guid UserId,
    int Count,
    DateTime SyncedAt);