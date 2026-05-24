namespace FinBot.Kafka.Abstractions.Providers;

/// <summary>
/// Нужен для атомарного выполнения сценария "получил-отправил", в DI его нет, используется только в ITransactionMessageHandler
/// </summary>
/// <remarks>
/// Возвращает синхронных продюсеров, так как транзакция идет отложенная
/// </remarks>
public interface IConsumeProduceContext: IProducerProvider
{
    
}