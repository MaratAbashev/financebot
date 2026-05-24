namespace FinBot.Kafka.Abstractions.Producers;

/// <summary>
/// Абстракция асихронного продюсера
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IAsyncProducer<in TKey, in TValue>: IProducer<TKey, TValue>
{
    Task ProduceAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Абстракция асихронного продюсера
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IAsyncProducer<in TValue>: IProducer<TValue>
{
    Task ProduceAsync(TValue value, CancellationToken cancellationToken = default);
}