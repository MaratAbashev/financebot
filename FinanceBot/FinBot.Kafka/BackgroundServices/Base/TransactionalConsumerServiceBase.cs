using Confluent.Kafka;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Impl.Providers;
using FinBot.Kafka.Internal.DI;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices.Base;

internal abstract class TransactionalConsumerServiceBase<TKey, TValue> : ConsumerServiceBase<TKey,TValue>
{
    private ConsumeProduceContext? _context;
    private readonly IProducer<byte[]?, byte[]> _producer;
    private readonly KafkaGlobalSettings _kafkaGlobalSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionalConsumerServiceBase<TKey, TValue>> _logger;

    protected TransactionalConsumerServiceBase(
        BackgroundServicesInitManager initManager,
        KafkaGlobalSettings kafkaGlobalSettings,
        ProducerSettingsGeneral producerSettingsGeneral,
        RegistrationConsumer<TKey, TValue> registrationConsumer,
        IServiceProvider serviceProvider,
        IAsyncProducerProvider producerProvider,
        ILogger<TransactionalConsumerServiceBase<TKey, TValue>> logger) : 
        base(initManager,registrationConsumer, producerProvider, logger)
    {
        _kafkaGlobalSettings = kafkaGlobalSettings;
        _serviceProvider = serviceProvider;
        _logger = logger;
        var config = new ProducerConfig().FromProducerSettingsGeneral(
            producerSettingsGeneral, kafkaGlobalSettings);
        config.TransactionalId = Guid.NewGuid().ToString();
        _producer = new ProducerBuilder<byte[]?, byte[]>(config)
            .Build();
        _producer.InitTransactions(kafkaGlobalSettings.TransactionInitDelay);
    }

    protected abstract Task HandleMessageWithContextAsync(
        ConsumeResult<TKey,TValue> consumeResult, 
        ConsumeProduceContext context, 
        CancellationToken cancellationToken);

    protected override async Task ExecuteAfterMessageHandleAsync(
        bool success, 
        ConsumeResult<TKey, TValue> consumeResult,
        CancellationToken cancellationToken)
    {
        if (_context != null && success)
        {
            try
            {
                await _context.ExecuteTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _context.AbortTransaction();
                _logger.LogError(ex, "Ошибка при транзакционном получении и отправке сообщения");
                throw;
            }
        }
    }

    protected override async Task HandleMessageAsync(
        ConsumeResult<TKey, TValue> consumeResult, 
        CancellationToken cancellationToken)
    {
        var nextOffset = new TopicPartitionOffset(
            consumeResult.TopicPartition, 
            consumeResult.Offset + 1
        );
        var offsetsToCommit = new List<TopicPartitionOffset> { nextOffset };
                
        _context = new ConsumeProduceContext(
            _kafkaGlobalSettings, 
            _serviceProvider, 
            _producer,
            Consumer.ConsumerGroupMetadata, 
            offsetsToCommit);
        
        await HandleMessageWithContextAsync(consumeResult, _context, cancellationToken);
    }
}