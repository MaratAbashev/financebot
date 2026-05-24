using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TopicResponse: ITopic
{
    public string TopicName => "test-response";
}