using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Events;
using FinBot.Domain.Utils;
using FinBot.Observability.Constants;
using FinBot.Observability.Metrics;
using FinBot.Observability.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinBot.Integrations.Kafka;

[Obsolete("Будет выпилено и заменено")]
public class KafkaProducer : IReportProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly BusinessMetrics _metrics;

    public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger, BusinessMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
        };
        _topic = config["Kafka:Topic"] ?? "finbot-reports";
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task<Result> QueueReportGenerationAsync(ReportGenerationEvent reportEvent)
    {
        using var activity = ActivitySources.FinBot.StartActivity(
            $"kafka.produce {_topic}", ActivityKind.Producer);
        activity?.SetTag(ObservabilityConstants.Tags.MessagingSystem, "kafka");
        activity?.SetTag(ObservabilityConstants.Tags.MessagingDestination, _topic);

        try
        {
            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(reportEvent),
                Headers = new Headers()
            };
            KafkaPropagation.InjectContext(activity, message.Headers);

            var deliveryResult = await _producer.ProduceAsync(_topic, message);
            activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaPartition, deliveryResult.Partition.Value);
            activity?.SetTag(ObservabilityConstants.Tags.MessagingKafkaOffset, deliveryResult.Offset.Value);

            _metrics.KafkaMessagesProduced.Add(1, new KeyValuePair<string, object?>("topic", _topic));
            _logger.LogInformation("Queued report generation: {ReportType} for Group {GroupId}",
                reportEvent.ReportType, reportEvent.GroupId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error producing Kafka message");
            return Result.Failure(ex.Message);
        }
    }
}