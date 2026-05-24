using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Reports;

/// <summary>
/// Детерминированное имя объекта в MinIO
/// </summary>
public static class ReportObjectName
{
    public static string Build(
        long userTgId,
        Guid groupId,
        ReportType reportType,
        ExcelType excelType,
        string periodKey)
    {
        var prefix = PrefixFor(excelType);
        var extension = ExtensionFor(excelType);

        var scope = reportType == ReportType.ForGroup
            ? $"group_{groupId}"
            : $"user_{userTgId}_group_{groupId}";

        return $"{prefix}_{scope}_{periodKey}.{extension}";
    }

    private static string PrefixFor(ExcelType type) => type switch
    {
        ExcelType.ExcelTable => "table",
        ExcelType.ColumnChart => "barChart",
        ExcelType.LineChart => "lineChart",
        _ => "report"
    };

    private static string ExtensionFor(ExcelType type) => type switch
    {
        ExcelType.ExcelTable => "xlsx",
        ExcelType.ColumnChart or ExcelType.LineChart => "png",
        _ => "bin"
    };
}
