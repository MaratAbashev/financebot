using FinBot.Kafka.Abstractions.Producers;

namespace FinBot.Kafka.Abstractions.Providers;

/// <summary>
/// Фабрика для асинхронных продюсеров, получать продюсера нужно только через него, то есть внедрять его в свои классы и тд
/// </summary>
public interface IAsyncProducerProvider
{
    /// <summary>
    /// Получить асинхронного продюсера
    /// </summary>
    /// <typeparam name="TKey">ключ продюсера</typeparam>
    /// <typeparam name="TValue">значение продюсера</typeparam>
    /// <typeparam name="TTopic">топик на который он подписан</typeparam>
    /// <returns>асинхронный продюсер</returns>
    IAsyncProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic;

    /// <summary>
    /// Получить асинхронного продюсера
    /// </summary>
    /// <typeparam name="TValue">значение продюсера</typeparam>
    /// <typeparam name="TTopic">топик на который он подписан</typeparam>
    /// <returns>асинхронный продюсер</returns>
    IAsyncProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic;
}