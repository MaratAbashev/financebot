namespace FinBot.Kafka.Abstractions.Producers;

/// <summary>
/// Абстракция синхронного продюсера
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IProducer<in TValue>
{
    void Produce(TValue value);
}

/// <summary>
/// Абстракция синхронного продюсера
/// </summary>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IProducer<in TKey, in TValue>
{
    void Produce(TKey key, TValue value);
}