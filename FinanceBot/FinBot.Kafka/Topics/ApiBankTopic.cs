using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для API, слушающего ответы от BankService
/// </summary>
public class ApiBankTopic: ITopic
{
    public string TopicName => "api-bank";
}