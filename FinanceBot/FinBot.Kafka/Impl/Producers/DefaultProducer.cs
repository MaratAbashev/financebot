using System.Diagnostics;
using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Utils;
using FinBot.Observability.Constants;
using FinBot.Observability.Tracing;

namespace FinBot.Kafka.Impl.Producers;

internal class DefaultProducer<TKey, TValue, TTopic>(
    ProducerSettings<TKey,TValue, TTopic> config,
    Confluent.Kafka.IProducer<byte[]?,byte[]> producer)
    : IAsyncProducer<TKey, TValue> where TTopic : ITopic
{
    private ISerializer<TValue> ValueSerializer => config.ValueSerializer;
    private ISerializer<TKey> KeySerializer => config.KeySerializer;
    public string Topic => config.Topic.TopicName;

    public async Task ProduceAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        using var activity = StartProduceActivity(Topic);
        var message = MessageHelper.GetDeserializedMessage(Topic, key, value, KeySerializer, ValueSerializer);
        message.Headers = new Headers();
        KafkaPropagation.InjectContext(activity, message.Headers);

        var deliveryResult = await producer.ProduceAsync(Topic, message, cancellationToken);
        SetDeliveryTags(activity, deliveryResult);
    }

    public void Produce(TKey key, TValue value)
    {
        using var activity = StartProduceActivity(Topic);
        var message = MessageHelper.GetDeserializedMessage(Topic, key, value, KeySerializer, ValueSerializer);
        message.Headers = new Headers();
        KafkaPropagation.InjectContext(activity, message.Headers);

        producer.Produce(Topic, message);
    }

    private static Activity? StartProduceActivity(string topic)
    {
        var activity = ActivitySources.FinBot.StartActivity($"kafka.produce {topic}", ActivityKind.Producer);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingSystem, "kafka");
        activity?.SetTag(ObservabilityConstants.Tags.MessagingDestination, topic);
        return activity;
    }

    private static void SetDeliveryTags(Activity? activity, DeliveryResult<byte[]?, byte[]> deliveryResult)
    {
        activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaPartition, deliveryResult.Partition.Value);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaOffset, deliveryResult.Offset.Value);
    }
}

internal class DefaultProducer<TValue, TTopic>(
    ProducerSettings<Null,TValue, TTopic> config,
    Confluent.Kafka.IProducer<byte[]?,byte[]> producer)
    : IAsyncProducer<TValue> where TTopic : ITopic
{
    private ISerializer<TValue> ValueSerializer => config.ValueSerializer;
    public string Topic => config.Topic.TopicName;

    public async Task ProduceAsync(TValue value, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.FinBot.StartActivity(
            $"kafka.produce {Topic}", ActivityKind.Producer);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingSystem, "kafka");
        activity?.SetTag(ObservabilityConstants.Tags.MessagingDestination, Topic);

        var message = MessageHelper.GetDeserializedMessage<object, TValue>(
            Topic, null, value, null, ValueSerializer);
        message.Headers = new Headers();
        KafkaPropagation.InjectContext(activity, message.Headers);

        var deliveryResult = await producer.ProduceAsync(Topic, message, cancellationToken);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaPartition, deliveryResult.Partition.Value);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaOffset, deliveryResult.Offset.Value);
    }

    public void Produce(TValue value)
    {
        using var activity = ActivitySources.FinBot.StartActivity(
            $"kafka.produce {Topic}", ActivityKind.Producer);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingSystem, "kafka");
        activity?.SetTag(ObservabilityConstants.Tags.MessagingDestination, Topic);

        var message = MessageHelper.GetDeserializedMessage<object, TValue>(
            Topic, null, value, null, ValueSerializer);
        message.Headers = new Headers();
        KafkaPropagation.InjectContext(activity, message.Headers);

        producer.Produce(Topic, message);
    }
}