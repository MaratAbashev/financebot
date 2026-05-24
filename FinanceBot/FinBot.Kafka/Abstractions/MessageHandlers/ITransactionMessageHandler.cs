using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Abstractions.MessageHandlers;

/// <summary>
/// Бизнесовая обработка сообщения, при добавлении консьюмера нужно реализовать
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface ITransactionMessageHandler<in TKey,in TValue>
{
    /// <summary>
    /// Обработать сообщение из кафки
    /// </summary>
    /// <param name="key">ключ сообщения</param>
    /// <param name="message">сообщение</param>
    /// <param name="context">транзакционный контекст, если нужно атомарно обработать сообщение и отправить обратно в кафку</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task HandleAsync(TKey key, TValue message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Бизнесовая обработка сообщения, при добавлении консьюмера нужно реализовать
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface ITransactionMessageHandler<in TValue>
{
    /// <summary>
    /// Обработать сообщение из кафки
    /// </summary>
    /// <param name="message">сообщение</param>
    /// <param name="context">транзакционный контекст, если нужно атомарно обработать сообщение и отправить обратно в кафку</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task HandleAsync(TValue message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default);
}