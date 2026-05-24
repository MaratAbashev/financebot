using FinBot.Kafka.Abstractions.Producers;

namespace FinBot.Kafka.Abstractions.Providers;

/// <summary>
/// Фабрика для синхронных продюсеров, получать продюсера нужно только через него, то есть внедрять его в свои классы и тд
/// </summary>
/// <remarks>
/// Лучше не использовать, нужен только для IConsumeProduceContext
/// </remarks>
public interface IProducerProvider
{
    IProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic;
    IProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic;
}