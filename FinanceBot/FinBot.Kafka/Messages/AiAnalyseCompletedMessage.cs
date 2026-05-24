namespace FinBot.Kafka.Messages;

public class AiAnalyseCompletedMessage
{
    public required string AnalysisCacheId { get; set; }
    public required long UserId { get; set; }
}