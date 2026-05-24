using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestErrorTransactionalMessageHandler: ITransactionMessageHandler<TestMessage>
{
    public int InvokedCount { get; private set; }
    public async Task HandleAsync(TestMessage message, IConsumeProduceContext context, CancellationToken cancellationToken = default)
    {
        InvokedCount++;
        throw new Exception();
    }
}