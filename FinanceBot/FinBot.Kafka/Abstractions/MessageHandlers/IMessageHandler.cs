namespace FinBot.Kafka.Abstractions.MessageHandlers;

/// <summary>
/// Бизнесовая обработка сообщения, при добавлении консьюмера нужно реализовать
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IMessageHandler<in TKey, in TValue>
{
    /// <summary>
    /// Обработать сообщение из кафки
    /// </summary>
    /// <param name="key">ключ сообщения</param>
    /// <param name="message">сообщение</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task HandleAsync(TKey key,TValue message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Бизнесовая обработка сообщения, при добавлении консьюмера нужно реализовать
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IMessageHandler<in TValue>
{
    /// <summary>
    /// Обработать сообщение из кафки
    /// </summary>
    /// <param name="message">сообщение</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task HandleAsync(TValue message, CancellationToken cancellationToken = default);
}