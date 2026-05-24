namespace FinBot.Kafka.Configuration;

public class KafkaGlobalSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan TransactionInitDelay { get; set; } = TimeSpan.FromSeconds(30);
    public short PartitionReplicationFactor { get; set; } = 2;
    public int PartitionNums { get; set; } = 2;
}