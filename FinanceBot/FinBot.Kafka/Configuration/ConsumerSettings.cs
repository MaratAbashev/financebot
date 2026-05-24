using Confluent.Kafka;
using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Configuration;

public class ConsumerSettings
{
    internal string GroupId { get; set; } = Guid.NewGuid().ToString();
    internal List<ITopic> Topics { get; set; } = new();
    public bool EnableAutoCommit { get; set; } = false;
    public int AutoCommitIntervalMs { get; set; } = 5000;
    public bool EnableAutoOffsetStore { get; set; } = false;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ErrorMessageHandleDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableDeadLetterQueue { get; set; } = true;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}