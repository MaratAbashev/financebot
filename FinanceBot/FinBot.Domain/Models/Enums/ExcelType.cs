namespace FinBot.Domain.Models.Enums;

/// <summary>
/// Тип Excel отчёта
/// </summary>
public enum ExcelType
{
    /// <summary>
    /// Табличка
    /// </summary>
    ExcelTable = 0,

    /// <summary>
    /// Столбчатая диаграмма по категориям
    /// </summary>
    ColumnChart = 1,

    /// <summary>
    /// Линейная диаграмма по дням
    /// </summary>
    LineChart = 2
}