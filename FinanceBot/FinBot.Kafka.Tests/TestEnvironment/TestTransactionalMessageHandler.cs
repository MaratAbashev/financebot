using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestTransactionalMessageHandler: ITransactionMessageHandler<TestMessage>
{
    public async Task HandleAsync(TestMessage message, IConsumeProduceContext context, CancellationToken cancellationToken = default)
    {
        if (message is not null)
        {
            var producer = context.GetProducer<TestMessage, TopicResponse>();
            message.Body = message.Body.ToUpper();
            producer.Produce(message);
        }
    }
}