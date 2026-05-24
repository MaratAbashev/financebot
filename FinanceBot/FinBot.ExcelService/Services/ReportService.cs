using FinBot.Domain.Models.Enums;
using FinBot.Domain.Options;
using FinBot.Domain.Reports;
using FinBot.Domain.Utils;
using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Repositories;
using FinBot.MinIOS3;
using Microsoft.Extensions.Options;

namespace FinBot.ExcelService.Services;

public class ReportService(
    IExpenseRepository expenseRepository,
    IReportBuilderFactory builderFactory,
    IMinioStorage storage,
    IOptions<StorageOptions> storageOptions,
    TimeProvider timeProvider,
    ILogger<ReportService> logger) : IReportService
{
    private readonly StorageOptions _storage = storageOptions.Value;

    public async Task<Result<string>> GenerateAndStoreAsync(ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = PeriodCalculator.ForPrevious(request.TimeInterval, timeProvider.GetUtcNow());
        var bucket = ResolveBucket(request.ExcelType);
        var objectName = ReportObjectName.Build(
            request.UserTgId, request.GroupId, request.ReportType, request.ExcelType, period.Key);

        logger.LogInformation(
            "Report request: {ExcelType}/{ReportType}/{TimeInterval} User={UserTgId} Group={GroupId} → {Bucket}/{Object}",
            request.ExcelType, request.ReportType, request.TimeInterval,
            request.UserTgId, request.GroupId, bucket, objectName);

        var existsResult = await storage.ExistsAsync(bucket, objectName, cancellationToken);
        if (!existsResult.IsSuccess)
            return existsResult.SameFailure<string>();

        if (existsResult.Data)
        {
            logger.LogInformation("Storage hit: {Object} already in {Bucket}, skipping generation", objectName, bucket);
            return Result<string>.Success(objectName);
        }

        var expensesResult = request.ReportType switch
        {
            ReportType.ForUser => await expenseRepository.GetForUserInGroupAsync(
                request.UserTgId, request.GroupId, period.From, period.To, cancellationToken),
            ReportType.ForGroup => await expenseRepository.GetForGroupAsync(
                request.GroupId, period.From, period.To, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.ReportType))
        };

        if (!expensesResult.IsSuccess)
            return expensesResult.SameFailure<string>();

        var expenses = expensesResult.Data;
        if (expenses.Count == 0)
            return Result<string>.Failure($"No expenses for period {period.Key}", ErrorType.NotFound);

        var builder = builderFactory.Get(request.ExcelType);
        var bytes = builder.Build(expenses, request);
        return await storage.UploadAsync(bucket, bytes, objectName, builder.ContentType, cancellationToken);
    }

    private string ResolveBucket(ExcelType type) => type switch
    {
        ExcelType.ExcelTable => _storage.ExcelTablesBucket,
        ExcelType.ColumnChart => _storage.BarChartsBucket,
        ExcelType.LineChart => _storage.LineChartsBucket,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
