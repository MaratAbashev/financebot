using Confluent.Kafka;

namespace FinBot.Kafka.Configuration;

public class ProducerSettingsGeneral
{
    public Acks Acks { get; set; } = Acks.All;
    public bool EnableIdempotence { get; set; } = true;
    public int MessageTimeoutMs { get; set; } = 30000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public int LingerMs { get; set; } = 5;
    public int BatchSize { get; set; } = 16384;
    public CompressionType CompressionType { get; set; } = CompressionType.Snappy;
    public int QueueBufferingMaxMessages { get; set; } = 100000;
}