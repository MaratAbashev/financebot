namespace FinBot.Domain.Options;

public class StorageOptions
{
    public string ExcelTablesBucket { get; set; } = null!;
    public string BarChartsBucket { get; set; } = null!;
    public string LineChartsBucket { get; set; } = null!;
}
