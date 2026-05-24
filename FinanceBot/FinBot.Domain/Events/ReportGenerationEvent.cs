using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Events;

public class ReportGenerationEvent
{
    public Guid GroupId { get; set; }
    public Guid? UserId { get; set; }
    public ExcelType ExcelType { get; set; }
    public ReportType ReportType { get; set; }
}