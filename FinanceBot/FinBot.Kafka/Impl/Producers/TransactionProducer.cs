using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Utils;

namespace FinBot.Kafka.Impl.Producers;

internal class TransactionProducer<TKey, TValue, TTopic>(
    ProducerSettings<TKey, TValue,TTopic> settings,
    IProducerRegisterManager producerRegisterManager)
    : Abstractions.Producers.IProducer<TKey, TValue> where TTopic : ITopic
{
    public string Topic => settings.Topic.TopicName;
    private ISerializer<TValue> ValueSerializer => settings.ValueSerializer;
    private ISerializer<TKey> KeySerializer => settings.KeySerializer;
    
    public void Produce(TKey key, TValue value)
    {
        var message = MessageHelper.GetDeserializedMessage(
            Topic, key, value, KeySerializer, ValueSerializer);
        
        producerRegisterManager.RegisterProducerOperation(Topic, message);
    }
}

internal class TransactionProducer<TValue,TTopic>(
    ProducerSettings<Null, TValue, TTopic> settings,
    IProducerRegisterManager producerRegisterManager)
    : Abstractions.Producers.IProducer<TValue> where TTopic : ITopic
{
    public string Topic => settings.Topic.TopicName;
    private ISerializer<TValue> ValueSerializer => settings.ValueSerializer;
    
    public void Produce(TValue value)
    {
        var message = MessageHelper.GetDeserializedMessage<object, TValue>(
            Topic, null, value, null, ValueSerializer);
        producerRegisterManager.RegisterProducerOperation(Topic, message);
    }
}