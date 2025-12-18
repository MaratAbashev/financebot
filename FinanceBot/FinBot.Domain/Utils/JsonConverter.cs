namespace FinBot.Domain.Utils;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),

            JsonTokenType.Number => ReadNumber(ref reader),

            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.StartArray  => ReadArray(ref reader, options),

            _ => throw new JsonException($"Unsupported token: {reader.TokenType}")
        };
    }

    private static object ReadNumber(ref Utf8JsonReader reader)
    {
        // Сначала пытаемся целое (как правило удобнее для id/счетчиков)
        if (reader.TryGetInt64(out var l)) return l;

        // Иначе double
        if (reader.TryGetDouble(out var d)) return d;

        // На всякий случай fallback
        return reader.GetDecimal();
    }

    private static Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dict;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var propName = reader.GetString()!;
            reader.Read(); // move to value

            dict[propName] = JsonSerializer.Deserialize<object?>(ref reader, options);
        }

        throw new JsonException("Incomplete JSON object.");
    }

    private static List<object?> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var list = new List<object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return list;

            list.Add(JsonSerializer.Deserialize<object?>(ref reader, options));
        }

        throw new JsonException("Incomplete JSON array.");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
