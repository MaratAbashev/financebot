using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для API, слушающего ответы от Excel
/// </summary>
public class ApiExcelTopic: ITopic
{
    public string TopicName => "api-excel";
}