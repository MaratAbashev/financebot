using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.KafkaMessages;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Topics;

namespace FinBot.Bll.Implementation.Services;

public class ReportService(IAsyncProducerProvider provider): IReportService
{
    public async Task<Result> GenerateReportAsync(long userTgId, Guid groupId, ReportType reportType, ExcelType excelType,
        TimeInterval timeInterval, CancellationToken cancellationToken = default)
    {
        var producer = provider.GetProducer<ExcelMessage, ExcelTopic>();
        var message = new ExcelMessage
        {
            UserTgId = userTgId,
            GroupId = groupId,
            ExcelType = excelType,
            ReportType = reportType,
            TimeInterval = timeInterval
        };
        try
        {
            await producer.ProduceAsync(message, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}