using Confluent.Kafka;

namespace FinBot.Kafka.Abstractions;

internal interface IProducerRegisterManager
{
    void RegisterProducerOperation(string topic, Message<byte[]?,byte[]> message);
}