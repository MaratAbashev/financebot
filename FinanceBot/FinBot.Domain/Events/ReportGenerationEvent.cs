namespace FinBot.Domain.Events;

public enum ReportType
{
    ExcelTable,
    CategoryChart,
    LineChart
}

public class ReportGenerationEvent
{
    public Guid GroupId { get; set; }
    public Guid? UserId { get; set; }
    public ReportType Type { get; set; }
}