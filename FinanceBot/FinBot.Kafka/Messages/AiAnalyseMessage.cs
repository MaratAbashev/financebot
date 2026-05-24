using FinBot.Domain.Models.Enums;

namespace FinBot.Kafka.Messages;

public class AiAnalyseMessage
{
    public long UserId { get; set; }
    public Guid GroupId { get; set; }
    public AnalyseMode AnalyseMode { get; set; }
    public TimeInterval TimeInterval { get; set; }
}

public enum AnalyseMode
{
    Analyse,
    Advice
}