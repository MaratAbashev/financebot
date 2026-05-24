namespace FinBot.Domain.KafkaMessages;

public class ExcelCompletedMessage
{
    public string? FileKey { get; set; }
    public string? ErrorMessage { get; set; }
    public required long UserTgId { get; set; }
    public bool IsNoExpensesForPeriod { get; set; }
}