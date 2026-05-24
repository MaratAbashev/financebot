using FinBot.Domain.Models.Enums;

namespace FinBot.ExcelService.Reports;

public record ReportRequest(
    long UserTgId,
    Guid GroupId,
    ReportType ReportType,
    ExcelType ExcelType,
    TimeInterval TimeInterval);
