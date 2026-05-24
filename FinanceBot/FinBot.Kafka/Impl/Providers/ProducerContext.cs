using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Impl.Producers;
using FinBot.Kafka.Internal.DI;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Kafka.Impl.Providers;

internal class ProducerContext : IProducerContext
{
    private readonly Confluent.Kafka.IProducer<byte[]?, byte[]> _producer;
    private readonly IServiceProvider _serviceProvider;

    public ProducerContext(
        KafkaGlobalSettings kafkaGlobalSettings,
        ProducerSettingsGeneral producerSettings, 
        IServiceProvider serviceProvider)
    {
        var config = new ProducerConfig().FromProducerSettingsGeneral(
            producerSettings, kafkaGlobalSettings);
        config.TransactionalId = Guid.NewGuid().ToString();
        _producer = new ProducerBuilder<byte[]?, byte[]>(config)
            .Build();
        //TODO внедрить пул продюсеров в случае если понадобится транзакционный контекст
        //TODO сейчас на каждый вызов будет тратиться минимум 5 секунд, чтобы инициализировать продюсера
        _producer.InitTransactions(TimeSpan.FromSeconds(5));
        _serviceProvider = serviceProvider;
    }

    public IAsyncProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<TKey, TValue, TTopic>)} не найден";
        var registrationProducer = _serviceProvider.GetService<RegistrationProducer<TKey, TValue,TTopic>>() 
                                   ?? throw new NullReferenceException(exceptionMessage);
        
        return new DefaultProducer<TKey, TValue, TTopic>(registrationProducer.Settings, _producer);
    }

    public IAsyncProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<Null,TValue, TTopic>)} не найден";
        var registrationProducer = _serviceProvider.GetService<RegistrationProducer<Null,TValue,TTopic>>() 
                                   ?? throw new NullReferenceException(exceptionMessage);
        
        return new DefaultProducer<TValue, TTopic>(registrationProducer.Settings, _producer);
    }

    public void BeginTransaction()
    {
        _producer.BeginTransaction();
    }

    public void CommitTransaction()
    {
        _producer.CommitTransaction();
    }

    public void AbortTransaction()
    {
        _producer.AbortTransaction();
    }
}