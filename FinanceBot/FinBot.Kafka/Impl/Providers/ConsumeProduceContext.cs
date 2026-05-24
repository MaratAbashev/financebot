using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Impl.Producers;
using FinBot.Kafka.Internal.DI;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Kafka.Impl.Providers;

internal class ConsumeProduceContext(
    KafkaGlobalSettings kafkaGlobalSettings,
    IServiceProvider serviceProvider,
    Confluent.Kafka.IProducer<byte[]?, byte[]> producer,
    IConsumerGroupMetadata metadata,
    IEnumerable<TopicPartitionOffset> offsets): 
    IConsumeProduceContext, 
    IProducerRegisterManager
{
    private readonly List<(string topic, Message<byte[]?,byte[]> message)> _operations = [];
    
    public Abstractions.Producers.IProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<TKey, TValue, TTopic>)} не найден";
        var registrationProducer = serviceProvider.GetService<RegistrationProducer<TKey, TValue,TTopic>>() 
                       ?? throw new NullReferenceException(exceptionMessage);
        return new TransactionProducer<TKey, TValue, TTopic>(
            registrationProducer.Settings, this);
    }

    public IProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<Null, TValue, TTopic>)} не найден";
        var registrationProducer = serviceProvider.GetService<RegistrationProducer<Null,TValue,TTopic>>() 
                                   ?? throw new NullReferenceException(exceptionMessage);
        return new TransactionProducer<TValue, TTopic>(
            registrationProducer.Settings, this);
    }

    public async Task ExecuteTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_operations.Count == 0) return;
        producer.BeginTransaction();
        foreach (var (topic, message) in _operations)
        {
            await producer.ProduceAsync(topic, message, cancellationToken);
        }
        producer.SendOffsetsToTransaction(offsets, metadata, kafkaGlobalSettings.OperationTimeout);
        producer.CommitTransaction();
    }

    public void AbortTransaction()
    {
        if (_operations.Count == 0) return;
        producer.AbortTransaction();
    }

    public void RegisterProducerOperation(string topic, Message<byte[]?,byte[]> message)
    {
        _operations.Add((topic, message));
    }
}