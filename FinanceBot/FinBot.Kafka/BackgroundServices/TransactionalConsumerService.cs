using Confluent.Kafka;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.BackgroundServices.Base;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Impl.Providers;
using FinBot.Kafka.Internal.DI;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices;

internal class TransactionalConsumerService<TKey, TValue, THandler>(
    BackgroundServicesInitManager initManager,
    KafkaGlobalSettings kafkaGlobalSettings,
    ProducerSettingsGeneral producerSettingsGeneral,
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    THandler handler,
    IServiceProvider serviceProvider,
    IAsyncProducerProvider producerProvider,
    ILogger<TransactionalConsumerService<TKey, TValue, THandler>> logger)
    : TransactionalConsumerServiceBase<TKey,TValue>(
        initManager,kafkaGlobalSettings, producerSettingsGeneral, registrationConsumer, 
        serviceProvider, producerProvider, logger) 
    where THandler : ITransactionMessageHandler<TKey, TValue>
{
    private THandler _handler = handler;

    protected override async Task HandleMessageWithContextAsync(ConsumeResult<TKey, TValue> consumeResult,
        ConsumeProduceContext context, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(
            consumeResult.Message.Key, 
            consumeResult.Message.Value, 
            context, 
            cancellationToken);
    }
}

internal class TransactionalConsumerService<TValue, THandler>(
    BackgroundServicesInitManager initManager,
    KafkaGlobalSettings kafkaGlobalSettings,
    ProducerSettingsGeneral producerSettingsGeneral,
    RegistrationConsumer<Null, TValue> registrationConsumer,
    THandler handler,
    IServiceProvider serviceProvider,
    IAsyncProducerProvider producerProvider,
    ILogger<TransactionalConsumerService<TValue, THandler>> logger)
    : TransactionalConsumerServiceBase<Null, TValue>( initManager,
        kafkaGlobalSettings, producerSettingsGeneral, registrationConsumer, 
        serviceProvider, producerProvider, logger)
    where THandler : ITransactionMessageHandler<TValue>
{
    private THandler _handler = handler;

    protected override async Task HandleMessageWithContextAsync(ConsumeResult<Null, TValue> consumeResult,
        ConsumeProduceContext context, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(
            consumeResult.Message.Value,
            context, 
            cancellationToken);
    }
}