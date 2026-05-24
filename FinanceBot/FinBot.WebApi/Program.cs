using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Interfaces.Services;
using FinBot.Cache;
using FinBot.Dal;
using FinBot.Domain.Options;
using FinBot.MinIOS3;
using FinBot.Observability;
using FinBot.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddObservability(configuration);


services.Configure<StorageOptions>(configuration.GetSection("Storage"));

services.PostConfigure<MinioOptions>(minio =>
{
    var storage = configuration.GetSection("Storage").Get<StorageOptions>()
                  ?? throw new InvalidOperationException("Storage section is missing");

    minio.Buckets =
    [
        storage.ExcelTablesBucket,
        storage.BarChartsBucket,
        storage.LineChartsBucket
    ];
});

services.AddPostgresDb(configuration)
    .AddTelegram(configuration)
    .AddOpenApi()
    .AddKafka(configuration)
    .AddRedisCacheIntegration(configuration)
    .AddHttpClients(configuration)
    .AddMinioS3(configuration);

services.AddScoped<IReportService, ReportService>();
services.Configure<StorageOptions>(configuration.GetSection("Storage"));
services.PostConfigure<MinioOptions>(minio =>
{
    var storage = configuration.GetSection("Storage").Get<StorageOptions>()
                  ?? throw new InvalidOperationException("Storage section is missing");

    minio.Buckets =
    [
        storage.ExcelTablesBucket,
        storage.BarChartsBucket,
        storage.LineChartsBucket
    ];
});

var app = builder.Build();

app.UseObservability();

app.Run();