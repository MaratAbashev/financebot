namespace FinBot.Kafka.Abstractions.Providers;

/// <summary>
/// Нужен для транзакционной отправки разных продюсеров в разные топики или нескольких сообщений в один
/// </summary>
/// <remarks>
/// Использовать пока не надо, но может понадобится
/// </remarks>
public interface IProducerContext: IAsyncProducerProvider
{
    void BeginTransaction();
    void CommitTransaction();
    void AbortTransaction();
}