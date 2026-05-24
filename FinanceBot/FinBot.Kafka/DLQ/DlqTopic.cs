using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.DLQ;

/// <summary>
/// Топик для DLQ
/// </summary>
/// <remarks>
/// не трогать, уже внедрено
/// </remarks>
public class DlqTopic: ITopic
{
    public string TopicName => "dlq";
}