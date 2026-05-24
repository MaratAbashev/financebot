using System.Diagnostics;
using System.Text.Json;
using FinBot.Observability.Constants;
using Serilog.Events;
using Serilog.Formatting;

namespace FinBot.Observability.Logging;

internal sealed class FinBotLogFormatter : ITextFormatter
{
    private static readonly HashSet<string> ReservedProperties =
    [
        ObservabilityConstants.LogProperties.Facility,
        ObservabilityConstants.LogProperties.MachineName,
        ObservabilityConstants.LogProperties.Endpoint,
        ObservabilityConstants.LogProperties.UniqueIdFacility
    ];

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            writer.WriteString("timestamp", logEvent.Timestamp.UtcDateTime.ToString("O"));
            writer.WriteString("level", logEvent.Level.ToString());
            writer.WriteString("message", logEvent.RenderMessage());

            writer.WriteString(
                ObservabilityConstants.LogProperties.TraceId,
                Activity.Current?.TraceId.ToString());

            WriteScalar(writer, logEvent, ObservabilityConstants.LogProperties.Facility);
            WriteScalar(writer, logEvent, ObservabilityConstants.LogProperties.MachineName);
            WriteScalar(writer, logEvent, ObservabilityConstants.LogProperties.Endpoint);
            WriteScalar(writer, logEvent, ObservabilityConstants.LogProperties.UniqueIdFacility);

            writer.WriteString(
                ObservabilityConstants.LogProperties.ExceptionMessage,
                logEvent.Exception?.Message);

            if (logEvent.Exception is not null)
            {
                writer.WriteString("exception", logEvent.Exception.ToString());
            }

            var extra = logEvent.Properties
                .Where(p => !ReservedProperties.Contains(p.Key))
                .ToList();

            if (extra.Count > 0)
            {
                writer.WriteStartObject("attributes");
                foreach (var prop in extra)
                {
                    writer.WritePropertyName(prop.Key);
                    WritePropertyValue(writer, prop.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        output.WriteLine(json);
    }

    private static void WriteScalar(Utf8JsonWriter writer, LogEvent logEvent, string key)
    {
        if (logEvent.Properties.TryGetValue(key, out var value) && value is ScalarValue { Value: { } v })
        {
            writer.WriteString(key, v.ToString());
        }
        else
        {
            writer.WriteNull(key);
        }
    }

    private static void WritePropertyValue(Utf8JsonWriter writer, LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                WriteScalarValue(writer, scalar.Value);
                break;
            case SequenceValue seq:
                writer.WriteStartArray();
                foreach (var item in seq.Elements)
                {
                    WritePropertyValue(writer, item);
                }

                writer.WriteEndArray();
                break;
            case StructureValue structure:
                writer.WriteStartObject();
                foreach (var prop in structure.Properties)
                {
                    writer.WritePropertyName(prop.Name);
                    WritePropertyValue(writer, prop.Value);
                }

                writer.WriteEndObject();
                break;
            case DictionaryValue dict:
                writer.WriteStartObject();
                foreach (var kv in dict.Elements)
                {
                    writer.WritePropertyName(kv.Key.Value?.ToString() ?? string.Empty);
                    WritePropertyValue(writer, kv.Value);
                }

                writer.WriteEndObject();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteScalarValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal m:
                writer.WriteNumberValue(m);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToString("O"));
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto.ToString("O"));
                break;
            case Guid g:
                writer.WriteStringValue(g);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}