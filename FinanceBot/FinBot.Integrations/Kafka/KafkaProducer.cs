using System.Text.Json;
using Confluent.Kafka;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Events;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinBot.Integrations.Kafka;

public class KafkaProducer : IReportProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
        };
        _topic = config["Kafka:Topic"] ?? "finbot-reports";
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task<Result> QueueReportGenerationAsync(ReportGenerationEvent reportEvent)
    {
        try
        {
            var message = JsonSerializer.Serialize(reportEvent);
            await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
            _logger.LogInformation($"Queued report generation: {reportEvent.Type} for Group {reportEvent.GroupId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing Kafka message");
            return Result.Failure(ex.Message);
        }
    }
}