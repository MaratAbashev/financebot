namespace FinBot.Kafka.Messages;

public class BankSyncCompletedMessage
{
    public Guid UserId { get; set; }
    public int NewTransactionsCount { get; set; }
    public DateTime SyncedAt { get; set; }
}