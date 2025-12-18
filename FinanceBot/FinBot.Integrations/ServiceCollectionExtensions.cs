using FinBot.Bll.Interfaces.Integration;
using FinBot.Integrations.Excel;
using FinBot.Integrations.Kafka;
using FinBot.Integrations.MinioS3;
using FinBot.Integrations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;

namespace FinBot.Integrations;

public static class ServiceCollectionExtensions
{
    public static void AddMinioS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithTimeout(30000)
                .Build();
        });
        services.Configure<MinioOptions>(configuration.GetSection("MinioOptions"));
        services.AddSingleton<IHostedService, MinioInitializer>();
        services.AddSingleton<IMinioStorage, MinioStorage>();
    }

    public static void AddGroupMetrics(this IServiceCollection services)
    {
        services.AddScoped<IExcelTableService, ExcelTableService>();
        services.AddScoped<IChartService, ChartService>();
    }
    
    public static void AddKafkaIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IReportProducer, KafkaProducer>();
    }
}