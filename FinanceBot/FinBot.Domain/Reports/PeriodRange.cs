namespace FinBot.Domain.Reports;

/// <summary>
/// Полуинтервал [From, To) и детерминированный ключ периода, пригодный для имени файла
/// </summary>
public record PeriodRange(DateTime From, DateTime To, string Key);
