using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для API, слушающего ответы от LLM
/// </summary>
public class ApiLlmTopic: ITopic
{
    public string TopicName => "api-llm";
}