using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для BankService, слушающего ответы от API
/// </summary>
public class BankTopic: ITopic
{
    public string TopicName => "bank";
}