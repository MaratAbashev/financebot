using FinBot.Domain.KafkaMessages;
using FinBot.Domain.Options;
using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Repositories;
using FinBot.ExcelService.Services;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;
using FinBot.MinIOS3;
using OfficeOpenXml;
using ScottPlot;

namespace FinBot.ExcelService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExcelReportPipeline(this IServiceCollection services,
        IConfiguration configuration)
    {
        ExcelPackage.License.SetNonCommercialPersonal("FinBot");

        SetupChartFont();

        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        services.AddScoped<IExpenseRepository, ExpenseRepository>();

        services.AddSingleton<IReportBuilder, ExcelTableBuilder>();
        services.AddSingleton<IReportBuilder, ColumnChartBuilder>();
        services.AddSingleton<IReportBuilder, LineChartBuilder>();
        services.AddSingleton<IReportBuilderFactory, ReportBuilderFactory>();

        services.AddScoped<IReportService, ReportService>();

        services.TryAddSingletonTimeProvider();

        return services;
    }

    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafka(settings =>
        {
            if (configuration["Kafka:BootstrapServers"] != null)
                settings.BootstrapServers = configuration["Kafka:BootstrapServers"]!;
        });
        services.AddProducerGeneral();
        services.AddProducer<ExcelCompletedMessage, ApiExcelTopic>();
        services.AddTransactionConsumer<ExcelMessage, ExcelMessageHandler, ExcelTopic>(configuration["Kafka:ExcelGroupId"]!);
        
        return services;
    }
    
    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(TimeProvider))) return;
        services.AddSingleton(TimeProvider.System);
    }

    private const string EmbeddedFontResource = "FinBot.ExcelService.Resources.Fonts.DejaVuSans.ttf";
    private const string ChartFontName = "FinBot Chart Font";

    private static void SetupChartFont()
    {
        var asm = typeof(ServiceCollectionExtensions).Assembly;
        using var stream = asm.GetManifestResourceStream(EmbeddedFontResource)
                           ?? throw new InvalidOperationException($"Embedded font '{EmbeddedFontResource}' not found.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"finbot-chart-{Guid.NewGuid():N}.ttf");
        using (var file = File.Create(tempPath))
            stream.CopyTo(file);

        Fonts.AddFontFile(ChartFontName, tempPath);
        Fonts.Default = ChartFontName;
    }
}