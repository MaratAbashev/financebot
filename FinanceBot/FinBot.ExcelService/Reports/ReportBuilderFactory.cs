using FinBot.Domain.Models.Enums;

namespace FinBot.ExcelService.Reports;

public interface IReportBuilderFactory
{
    IReportBuilder Get(ExcelType type);
}

public class ReportBuilderFactory : IReportBuilderFactory
{
    private readonly Dictionary<ExcelType, IReportBuilder> _builders;

    public ReportBuilderFactory(IEnumerable<IReportBuilder> builders)
    {
        _builders = builders.ToDictionary(b => b.Type);
    }

    public IReportBuilder Get(ExcelType type)
    {
        if (!_builders.TryGetValue(type, out var builder))
            throw new ArgumentOutOfRangeException(nameof(type), type, "No builder registered for this report type");

        return builder;
    }
}
