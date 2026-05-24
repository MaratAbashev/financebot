using Confluent.Kafka;
using FinBot.Kafka.Configuration;

namespace FinBot.Kafka.Extensions;

public static class SettingsExtensions
{
    public static ConsumerConfig FromConsumerSettings(this ConsumerConfig config, 
        ConsumerSettings settings, KafkaGlobalSettings globalSettings)
    {
        config.BootstrapServers = globalSettings.BootstrapServers;
        config.IsolationLevel = settings.IsolationLevel;
        config.EnableAutoCommit = settings.EnableAutoCommit;
        config.GroupId = settings.GroupId;
        config.AutoCommitIntervalMs = settings.AutoCommitIntervalMs;
        config.EnableAutoOffsetStore = settings.EnableAutoOffsetStore;
        config.AutoOffsetReset = settings.AutoOffsetReset;
        
        return config;
    }

    public static ProducerConfig FromProducerSettingsGeneral(this ProducerConfig config,
        ProducerSettingsGeneral settings, KafkaGlobalSettings globalSettings)
    {
        config.BootstrapServers = globalSettings.BootstrapServers;
        config.CompressionType = settings.CompressionType;
        config.EnableIdempotence = settings.EnableIdempotence;
        config.Acks = settings.Acks;
        config.BatchSize = settings.BatchSize;
        config.LingerMs = settings.LingerMs;
        config.QueueBufferingMaxMessages = settings.QueueBufferingMaxMessages;
        config.MessageTimeoutMs = settings.MessageTimeoutMs;
        config.RequestTimeoutMs = settings.RequestTimeoutMs;

        return config;
    }
}