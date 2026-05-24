using FinBot.Kafka.Abstractions.MessageHandlers;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestErrorMessageHandler: IMessageHandler<TestMessage>
{
    public int InvokedCount { get; private set; }
    public Task HandleAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        InvokedCount++;
        throw new Exception();
    }
}