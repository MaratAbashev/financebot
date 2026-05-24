using FinBot.Domain.KafkaMessages;
using FinBot.Domain.Utils;
using FinBot.ExcelService.Reports;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Topics;
using FinBot.Observability.Metrics;

namespace FinBot.ExcelService.Services;

public class ExcelMessageHandler(
    IServiceScopeFactory serviceScopeFactory,
    BusinessMetrics businessMetrics)
    : ITransactionMessageHandler<ExcelMessage>
{
    public async Task HandleAsync(ExcelMessage message, IConsumeProduceContext context, CancellationToken cancellationToken = default)
    {
        var scope = serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

        var reportRequest = new ReportRequest(
            message.UserTgId,
            message.GroupId,
            message.ReportType,
            message.ExcelType,
            message.TimeInterval);
        var result = await reportService.GenerateAndStoreAsync(reportRequest, cancellationToken);

        var outcome = result.IsSuccess ? "generated"
            : result.ErrorType == ErrorType.NotFound ? "not_found"
            : "error";
        businessMetrics.ExcelOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

        var producer = context.GetProducer<ExcelCompletedMessage, ApiExcelTopic>();
        var completedMessage = new ExcelCompletedMessage
        {
            FileKey = result.IsSuccess ? result.Data : null,
            ErrorMessage = result.ErrorMessage,
            UserTgId = message.UserTgId,
            IsNoExpensesForPeriod = result.ErrorType == ErrorType.NotFound
        };
        producer.Produce(completedMessage);
    }
}