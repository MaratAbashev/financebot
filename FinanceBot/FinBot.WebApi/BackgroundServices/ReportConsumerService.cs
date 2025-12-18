using System.Text.Json;
using Confluent.Kafka;
using FinBot.Bll.Interfaces;
using FinBot.Domain.Events;

namespace FinBot.WebApi.BackgroundServices;

public class ReportConsumerService : BackgroundService
{
    private readonly ILogger<ReportConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly string _topic;

    public ReportConsumerService(
        ILogger<ReportConsumerService> logger, 
        IServiceScopeFactory scopeFactory, 
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
        _topic = _config["Kafka:Topic"] ?? "finbot-reports";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = _config["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false 
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var reportEvent = JsonSerializer.Deserialize<ReportGenerationEvent>(consumeResult.Message.Value);

                    if (reportEvent != null)
                    {
                        await ProcessReportAsync(reportEvent);
                    }
                    
                    consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                }
            }
            consumer.Close();
        }, stoppingToken);
    }

    private async Task ProcessReportAsync(ReportGenerationEvent reportEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationsService>();

        _logger.LogInformation($"Processing report: {reportEvent.Type}");

        switch (reportEvent.Type)
        {
            case ReportType.ExcelTable:
                if (reportEvent.UserId.HasValue)
                    await integrationService.GenerateExcelTableForUserInGroup(reportEvent.UserId.Value, reportEvent.GroupId);
                else
                    await integrationService.GenerateExcelTableForGroup(reportEvent.GroupId);
                break;

            case ReportType.CategoryChart:
                if (reportEvent.UserId.HasValue)
                    await integrationService.GenerateDiagramForUserInGroup(reportEvent.UserId.Value, reportEvent.GroupId);
                else
                    await integrationService.GenerateDiagramForGroup(reportEvent.GroupId);
                break;

            case ReportType.LineChart:
                if (reportEvent.UserId.HasValue)
                    await integrationService.GenerateLineChartForUserInGroup(reportEvent.UserId.Value, reportEvent.GroupId);
                else
                    await integrationService.GenerateLineChartForGroup(reportEvent.GroupId);
                break;
        }
    }
}