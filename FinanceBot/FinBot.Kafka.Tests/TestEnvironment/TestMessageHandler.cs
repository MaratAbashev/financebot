using FinBot.Kafka.Abstractions.MessageHandlers;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestMessageHandler: IMessageHandler<TestMessage>
{
    public TestMessage? Message { get; private set; }
    public Task HandleAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        Message = message;
        return Task.CompletedTask;
    }
}