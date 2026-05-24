using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

/// <summary>
/// Топик для LLM, слушающего ответы от API
/// </summary>
public class LlmTopic: ITopic
{
    public string TopicName => "llm";
}