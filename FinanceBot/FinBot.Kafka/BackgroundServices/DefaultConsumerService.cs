using Confluent.Kafka;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.BackgroundServices.Base;
using FinBot.Kafka.Internal.DI;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices;

internal class DefaultConsumerService<TKey, TValue, THandler>(
    BackgroundServicesInitManager initManager,
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    THandler handler,
    IAsyncProducerProvider producerProvider,
    ILogger<DefaultConsumerService<TKey, TValue, THandler>> logger)
    : DefaultConsumerServiceBase<TKey,TValue>
        (initManager, registrationConsumer, producerProvider, logger)
    where THandler : IMessageHandler<TKey, TValue>
{
    private THandler _handler = handler;

    protected override async Task HandleMessageAsync(
        ConsumeResult<TKey, TValue> consumerResult,
        CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(
            consumerResult.Message.Key, 
            consumerResult.Message.Value, 
            cancellationToken);
    }
}

internal class DefaultConsumerService<TValue, THandler>(
    BackgroundServicesInitManager initManager,
    RegistrationConsumer<Null, TValue> registrationConsumer,
    THandler handler,
    IAsyncProducerProvider producerProvider,
    ILogger<DefaultConsumerService<TValue, THandler>> logger)
    : DefaultConsumerServiceBase<Null, TValue>(
        initManager,registrationConsumer, producerProvider, logger)
    where THandler : IMessageHandler<TValue>
{
    private THandler _handler = handler;

    protected override async Task HandleMessageAsync(
        ConsumeResult<Null, TValue> consumerResult, 
        CancellationToken cancellationToken)
    {
       await _handler.HandleAsync(
           consumerResult.Message.Value, 
           cancellationToken);
    }
}