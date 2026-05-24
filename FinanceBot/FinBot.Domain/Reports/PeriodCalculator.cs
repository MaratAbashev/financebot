using System.Globalization;
using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Reports;

public static class PeriodCalculator
{
    public static PeriodRange ForPrevious(TimeInterval interval, DateTimeOffset now) =>
        interval switch
        {
            TimeInterval.Day => Day(now.UtcDateTime),
            TimeInterval.Week => Week(now.UtcDateTime),
            TimeInterval.Month => Month(now.UtcDateTime),
            _ => throw new ArgumentOutOfRangeException(nameof(interval))
        };

    private static PeriodRange Day(DateTime nowUtc)
    {
        var today = new DateTime(nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            0,
            0,
            0,
            DateTimeKind.Utc);
        var yesterday = today.AddDays(-1);
        return new PeriodRange(yesterday,
            today,
            $"day_{yesterday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }

    private static PeriodRange Week(DateTime nowUtc)
    {
        var today = new DateTime(nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            0,
            0,
            0,
            DateTimeKind.Utc);
        var dow = (int)today.DayOfWeek;
        if (dow == 0) dow = 7;
        var currentMonday = today.AddDays(-(dow - 1));
        var prevMonday = currentMonday.AddDays(-7);

        var weekNum = ISOWeek.GetWeekOfYear(prevMonday);
        var weekYear = ISOWeek.GetYear(prevMonday);

        return new PeriodRange(prevMonday,
            currentMonday,
            $"week_{weekYear}-W{weekNum.ToString("D2", CultureInfo.InvariantCulture)}");
    }

    private static PeriodRange Month(DateTime nowUtc)
    {
        var firstThis = new DateTime(nowUtc.Year,
            nowUtc.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);
        var firstPrev = firstThis.AddMonths(-1);
        return new PeriodRange(firstPrev,
            firstThis,
            $"month_{firstPrev.ToString("yyyy-MM", CultureInfo.InvariantCulture)}");
    }
}
