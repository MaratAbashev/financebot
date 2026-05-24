using FinBot.BankService.Services;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;

namespace FinBot.BankService.Consumers;

public class BankSyncRequestHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<BankSyncRequestHandler> logger) : ITransactionMessageHandler<BankSyncMessage>
{
    public async Task HandleAsync(
        BankSyncMessage message,
        IConsumeProduceContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Синхронизация запрошена для UserId={UserId}", message.UserId);

            await using var scope = scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IBankSyncService>();

            var newCount = await syncService.SyncUserAsync(message.UserId, cancellationToken);

            var producer = context.GetProducer<BankSyncCompletedMessage, ApiBankTopic>();
            producer.Produce(new BankSyncCompletedMessage
            {
                UserId = message.UserId,
                NewTransactionsCount = newCount,
                SyncedAt = DateTime.UtcNow
            });

            logger.LogInformation(
                "Синхронизация завершена для UserId={UserId}, новых транзакций: {Count}",
                message.UserId, newCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка синхронизации для UserId={UserId}", message.UserId);
        }
    }
}