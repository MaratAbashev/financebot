using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для Excel, слушающего ответы от API
/// </summary>
public class ExcelTopic: ITopic
{
    public string TopicName => "excel";
}