namespace FinBot.Domain.Models.Enums;

/// <summary>
/// Период, за который строится отчёт. Берётся предыдущий завершённый период
/// </summary>
public enum TimeInterval
{
    /// <summary>
    /// Предыдущий календарный день
    /// </summary>
    Day = 0,

    /// <summary>
    /// Предыдущая ISO-неделя
    /// </summary>
    Week = 1,

    /// <summary>
    /// Предыдущий календарный месяц.
    ///</summary>
    Month = 2
}