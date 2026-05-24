using System.Text.Json;
using Confluent.Kafka;

namespace FinBot.Kafka.Utils;

public class JsonDeserializer<T>(JsonSerializerOptions? options = null) : IDeserializer<T>
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions();

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull) return default!;
        
        return JsonSerializer.Deserialize<T>(data, _options)!;
    }
}