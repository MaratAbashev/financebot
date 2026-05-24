using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Tests.TestEnvironment;

public class TestTopic: ITopic
{
    public string TopicName => "test";
}