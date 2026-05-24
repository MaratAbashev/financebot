using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace FinBot.Kafka.Utils;

public class JsonSerializer<T>(JsonSerializerOptions? options = null) : ISerializer<T>
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions();

    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null) return [];
        
        var json = JsonSerializer.Serialize(data, _options);
        return Encoding.UTF8.GetBytes(json);
    }
}