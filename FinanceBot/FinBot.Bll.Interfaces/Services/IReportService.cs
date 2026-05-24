using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IReportService
{
    Task<Result> GenerateReportAsync(
        long userTgId,
        Guid groupId,
        ReportType reportType,
        ExcelType excelType,
        TimeInterval timeInterval,
        CancellationToken cancellationToken = default);
}