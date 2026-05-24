using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestService(IAsyncProducerProvider factory)
{
    public async Task ProduceMessage(TestMessage message)
    {
        var producer = factory.GetProducer<TestMessage, TestTopic>();
        await producer.ProduceAsync(message);
    }
}