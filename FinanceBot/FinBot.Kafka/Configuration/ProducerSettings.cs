using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Utils;

namespace FinBot.Kafka.Configuration;

internal class ProducerSettings<TKey, TValue, TTopic> where TTopic : ITopic
{
    internal ProducerSettingsGeneral GeneralSettings { get; set; } = new();
    internal TTopic Topic { get; set; } = default!;
    internal ISerializer<TKey> KeySerializer { get; set; } = new JsonSerializer<TKey>();
    internal ISerializer<TValue> ValueSerializer { get; set; } = new JsonSerializer<TValue>();
}