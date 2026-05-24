using FinBot.Domain.Utils;
using FinBot.ExcelService.Reports;

namespace FinBot.ExcelService.Services;

public interface IReportService
{
    Task<Result<string>> GenerateAndStoreAsync(ReportRequest request, CancellationToken cancellationToken = default);
}
