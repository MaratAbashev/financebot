using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;

namespace FinBot.ExcelService.Reports;

public interface IReportBuilder
{
    ExcelType Type { get; }
    string ContentType { get; }

    byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request);
}
