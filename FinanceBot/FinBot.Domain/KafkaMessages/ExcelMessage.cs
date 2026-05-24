using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.KafkaMessages;

public class ExcelMessage
{
    public required long UserTgId { get; set; }
    public required Guid GroupId { get; set; }
    public required ReportType ReportType { get; set; }
    public required ExcelType ExcelType { get; set; }
    public required TimeInterval TimeInterval { get; set; }
}