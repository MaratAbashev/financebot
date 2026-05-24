using FinBot.Dal;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using FinBot.ExcelService;
using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Services;
using FinBot.MinIOS3;
using FinBot.Observability;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddObservability(configuration);

builder.Services
    .AddMinioS3(configuration, addInitializer: false)
    .AddReadDb(configuration)
    .AddExcelReportPipeline(configuration)
    .AddKafka(configuration);

var app = builder.Build();

app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.MapPost("/reports", async (
    long userTgId, Guid groupId, ReportType reportType, ExcelType excelType, TimeInterval timeInterval,
    IReportService reports, CancellationToken ct) =>
{
    var result = await reports.GenerateAndStoreAsync(
        new ReportRequest(userTgId, groupId, reportType, excelType, timeInterval), ct);

    return result.IsSuccess
        ? Results.Ok(new { fileKey = result.Data })
        : result.ErrorType == ErrorType.NotFound
            ? Results.NotFound(result.ErrorMessage)
            : Results.Problem(result.ErrorMessage);
});

app.Run();
