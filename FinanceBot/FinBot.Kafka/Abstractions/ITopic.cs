namespace FinBot.Kafka.Abstractions;

/// <summary>
/// Топик
/// </summary>
public interface ITopic
{
    /// <summary>
    /// Название топика
    /// </summary>
    public string TopicName { get; }
}