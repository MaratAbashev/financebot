using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace FinBot.Integrations.Services;

public class ChartService(IGenericRepository<Expense, int, PDbContext> repository) : IChartService
{
    public async Task<Result<byte[]>> GenerateCategoryChartForGroupAsync(Guid groupId)
    {
        var expenses = await GetGroupExpensesAsync(groupId);
        return GenerateCategoryBarChart(expenses, "Траты по категориям (Группа)");
    }

    public async Task<Result<byte[]>> GenerateCategoryChartForUserInGroupAsync(Guid groupId, Guid userId)
    {
        var expenses = await GetUserExpensesAsync(groupId, userId);
        return GenerateCategoryBarChart(expenses, "Траты по категориям (Личные)");
    }

    public async Task<Result<byte[]>> GenerateSpendingDiagramForGroupAsync(Guid groupId)
    {
        var expenses = await GetGroupExpensesAsync(groupId);
        return GenerateDailyLineChart(expenses, "Динамика трат по дням (Группа)");
    }

    public async Task<Result<byte[]>> GenerateSpendingDiagramForUserInGroupAsync(Guid groupId, Guid userId)
    {
        var expenses = await GetUserExpensesAsync(groupId, userId);
        return GenerateDailyLineChart(expenses, "Динамика трат по дням (Личные)");
    }


    private async Task<List<Expense>> GetGroupExpensesAsync(Guid groupId)
    {
        var startDate = DateTime.Today.AddDays(-DateTime.Today.Day + 1).ToUniversalTime();

        return await repository.GetAll()
            .Include(e => e.Account)
                .ThenInclude(a => a!.User)
            .Where(e => e.Account!.GroupId == groupId && e.Date >= startDate)
            .OrderByDescending(e => e.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Expense>> GetUserExpensesAsync(Guid groupId, Guid userId)
    {
        var startDate = DateTime.Today.AddDays(-DateTime.Today.Day + 1).ToUniversalTime();

        return await repository.GetAll()
            .Include(e => e.Account)
                .ThenInclude(a => a!.User)
            .Where(e => e.Account!.GroupId == groupId && e.Account!.UserId == userId && e.Date >= startDate)
            .OrderByDescending(e => e.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    private Result<byte[]> GenerateCategoryBarChart(List<Expense> expenses, string title)
    {
        if (expenses.Count == 0) return Result<byte[]>.Success(Array.Empty<byte>());

        Plot plot = new();

        var fontName = Fonts.Detect(title);
        if (!string.IsNullOrEmpty(fontName))
        {
            plot.Axes.Title.Label.FontName = fontName;
            plot.Axes.Bottom.TickLabelStyle.FontName = fontName;
            plot.Axes.Left.TickLabelStyle.FontName = fontName;
        }

        plot.Title(title);

        var categoryGroups = expenses
            .GroupBy(e => e.Category)
            .OrderBy(g => g.Key)
            .ToList();

        var users = expenses
            .Select(e => e.Account!.User!.DisplayName)
            .Distinct()
            .OrderBy(u => u)
            .ToList();
        
        var palette = Colors.Category10;
        List<Bar> allBars = new();
        List<(string Text, double X, double Y)> valueLabels = new();

        double groupWidth = 0.8;
        double barWidth = groupWidth / Math.Max(users.Count, 1);

        double[] tickPositions = new double[categoryGroups.Count];
        string[] tickLabels = new string[categoryGroups.Count];

        for (int i = 0; i < categoryGroups.Count; i++)
        {
            var categoryGroup = categoryGroups[i];
            double centerPosition = i;

            tickPositions[i] = centerPosition;
            tickLabels[i] = categoryGroup.Key.ToString();

            for (int u = 0; u < users.Count; u++)
            {
                var userName = users[u];
                var userSum = categoryGroup
                    .Where(e => (e.Account!.User!.DisplayName ?? "Unknown") == userName)
                    .Sum(e => e.Amount);

                if (userSum > 0)
                {
                    double offset = (u - (users.Count - 1) / 2.0) * barWidth;
                    
                    var bar = new Bar
                    {
                        Position = centerPosition + offset,
                        Value = (double)userSum,
                        FillColor = palette[u % palette.Length],
                        Size = barWidth,
                        LineWidth = 1,
                        Label = categoryGroup.Key + " " + userName
                    };
                    allBars.Add(bar);

                    valueLabels.Add((userSum.ToString("N0"), centerPosition + offset, (double)userSum));
                }
            }
        }

        var barPlot = plot.Add.Bars(allBars);

        foreach (var label in valueLabels)
        {
            var text = plot.Add.Text(label.Text, label.X, label.Y);
            text.LabelAlignment = Alignment.LowerCenter;
            text.Color = Colors.Black;

            if (!string.IsNullOrEmpty(fontName))
            {
                text.LabelFontName = fontName;
            }
        }

        for (int u = 0; u < users.Count; u++)
        {
            var marker = plot.Add.Marker(0, 0);
            marker.Color = palette[u % palette.Length];
            marker.LegendText = users[u];
            marker.Size = 0;
        }

        plot.ShowLegend();
        plot.Axes.Bottom.TickGenerator = new NumericManual(tickPositions, tickLabels);
        plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

        plot.Axes.Margins(bottom: 0, top: 0.15); 

        return Result<byte[]>.Success(plot.GetImageBytes(1200, 800, ImageFormat.Png));
    }

    private Result<byte[]> GenerateDailyLineChart(List<Expense> expenses, string title)
    {
        if (expenses.Count == 0) return Result<byte[]>.Success(Array.Empty<byte>());

        Plot plot = new();

        var fontName = Fonts.Detect(title);
        if (!string.IsNullOrEmpty(fontName))
        {
            plot.Axes.Title.Label.FontName = fontName;
            plot.Axes.Bottom.TickLabelStyle.FontName = fontName;
            plot.Axes.Left.TickLabelStyle.FontName = fontName;
        }

        plot.Title(title);
        plot.Axes.DateTimeTicksBottom();

        var users = expenses.Select(e => e.Account!.User!.DisplayName).Distinct().OrderBy(u => u).ToList();
        var palette = Colors.Category10;

        for (int i = 0; i < users.Count; i++)
        {
            var userName = users[i];
            var userExpenses = expenses
                .Where(e => (e.Account!.User!.DisplayName) == userName)
                .GroupBy(e => e.Date.Date)
                .ToDictionary(g => g.Key, g => (double)g.Sum(e => e.Amount));

            List<DateTime> dates = new();
            List<double> values = new();

            var minDate = expenses.Min(e => e.Date).Date;
            var maxDate = expenses.Max(e => e.Date).Date;

            for (var date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                dates.Add(date);
                values.Add(userExpenses.GetValueOrDefault(date, 0));
            }

            var scatter = plot.Add.Scatter(dates.ToArray(), values.ToArray());
            scatter.LegendText = userName;
            scatter.Color = palette[i % palette.Length];
            scatter.LineWidth = 3;
            scatter.MarkerSize = 7;
        }

        plot.ShowLegend();

        return Result<byte[]>.Success(plot.GetImageBytes(1200, 800, ImageFormat.Png));
    }
}
