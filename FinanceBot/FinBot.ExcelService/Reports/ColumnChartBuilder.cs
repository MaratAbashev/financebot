using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using ScottPlot;
using ScottPlot.Palettes;

namespace FinBot.ExcelService.Reports;

public class ColumnChartBuilder : IReportBuilder
{
    public ExcelType Type => ExcelType.ColumnChart;
    public string ContentType => "image/png";

    public byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request)
    {
        var grouped = expenses
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key.ToString(), Total = (double)g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .ToList();

        var plot = new Plot();
        plot.Title("Расходы по категориям");
        plot.YLabel("Сумма, ₽");

        var palette = new Category10();

        var bars = grouped
            .Select((g, i) => new Bar
            {
                Position = i,
                Value = g.Total,
                Label = $"{g.Total:N0} ₽",
                FillColor = palette.GetColor(i),
                LineColor = Colors.Black.WithAlpha(0.2),
                LineWidth = 1
            })
            .ToList();

        var barPlot = plot.Add.Bars(bars);
        barPlot.ValueLabelStyle.Bold = true;
        barPlot.ValueLabelStyle.FontSize = 13;

        var positions = Enumerable.Range(0, grouped.Count).Select(i => (double)i).ToArray();
        var labels = grouped.Select(g => g.Category).ToArray();
        plot.Axes.Bottom.SetTicks(positions, labels);
        plot.Axes.Bottom.TickLabelStyle.FontSize = 12;

        plot.Axes.Margins(bottom: 0, top: 0.25);

        return plot.GetImageBytes(1000, 600, ImageFormat.Png);
    }
}
