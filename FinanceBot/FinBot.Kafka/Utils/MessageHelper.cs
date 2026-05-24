using Confluent.Kafka;

namespace FinBot.Kafka.Utils;

internal static class MessageHelper
{
    public static Message<byte[]?,byte[]> GetDeserializedMessage<TKey, TValue>(
        string topic,
        TKey? key, 
        TValue value,
        ISerializer<TKey>? keySerializer,
        ISerializer<TValue> valueSerializer)
    {
        var serializedKey = key is not null  && keySerializer != null
            ? keySerializer.Serialize(key, new SerializationContext(
                MessageComponentType.Key, topic))
            : null;
        var serializedValue = 
            valueSerializer.Serialize(value, new SerializationContext(
                MessageComponentType.Value, topic));

        var message = new Message<byte[]?, byte[]>
        {
            Key = serializedKey, 
            Value = serializedValue
        };

        return message;
    }
}