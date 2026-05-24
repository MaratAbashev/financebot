using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace FinBot.Observability.Tracing;

public static class KafkaPropagation
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void InjectContext(Activity? activity, Headers headers)
    {
        if (activity is null)
        {
            return;
        }

        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            headers,
            HeaderSetter);
    }

    public static PropagationContext ExtractContext(Headers? headers)
    {
        if (headers is null)
        {
            return default;
        }

        return Propagator.Extract(default, headers, HeaderGetter);
    }

    private static void HeaderSetter(Headers headers, string key, string value)
    {
        headers.Remove(key);
        headers.Add(key, Encoding.UTF8.GetBytes(value));
    }

    private static IEnumerable<string> HeaderGetter(Headers headers, string key)
    {
        if (headers.TryGetLastBytes(key, out var bytes))
        {
            yield return Encoding.UTF8.GetString(bytes);
        }
    }
}