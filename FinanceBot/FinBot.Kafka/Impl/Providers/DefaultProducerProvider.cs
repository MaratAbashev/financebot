using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Impl.Producers;
using FinBot.Kafka.Internal.DI;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Kafka.Impl.Providers;

internal class DefaultProducerProvider(
    IServiceProvider serviceProvider)
    : IAsyncProducerProvider
{
    public IAsyncProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<TKey, TValue, TTopic>)} не найден";
        var registrationProducer = serviceProvider.GetService<RegistrationProducer<TKey, TValue,TTopic>>() 
                                   ?? throw new NullReferenceException(exceptionMessage);
        
        return new DefaultProducer<TKey, TValue, TTopic>(
            registrationProducer.Settings, registrationProducer.Producer);
    }

    public IAsyncProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic
    {
        var exceptionMessage = $"Продюсер {nameof(RegistrationProducer<Null,TValue, TTopic>)} не найден";
        var registrationProducer = serviceProvider.GetService<RegistrationProducer<Null,TValue,TTopic>>() 
                                   ?? throw new NullReferenceException(exceptionMessage);
        
        return new DefaultProducer<TValue, TTopic>(
            registrationProducer.Settings, registrationProducer.Producer);
    }
}